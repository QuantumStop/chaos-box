
namespace Editor.NodeEditor;

/// <summary>
/// Base class for graph editor connection line styles, e.g. curvy or angular.
/// </summary>
public abstract class ConnectionStyle
{
	/// <summary>
	/// Default connection style if not overridden by <see cref="GraphView.ConnectionStyle"/>.
	/// </summary>
	public static ConnectionStyle Default { get; } = new ClassicConnectionStyle();

	[field: ThreadStatic]
	private static List<ConnectionHandleConfig> HandleConfigs { get; set; }

	private List<ConnectionHandleConfig> _handleConfigs;

	/// <summary>
	/// Updates the handles, bounds, and path of a connection.
	/// </summary>
	/// <param name="connection">Connection to perform a layout for.</param>
	/// <param name="sceneStart">Connection start position in the scene.</param>
	/// <param name="sceneEnd">Connection end position in the scene.</param>
	public void Layout( Connection connection, Vector2 sceneStart, Vector2 sceneEnd )
	{
		_handleConfigs = HandleConfigs ??= new();
		_handleConfigs.Clear();

		try
		{
			OnPreLayout( connection, sceneStart, sceneEnd );
			OnConfigureHandles( connection, sceneStart, sceneEnd );

			var rect = OnGetBounds( connection, sceneStart, sceneEnd );

			connection.UpdateSceneBounds( rect );

			connection.SetHandles( _handleConfigs );
			connection.Clear();

			OnLayout( connection, sceneStart, sceneEnd );

			connection.Update();
		}
		finally
		{
			_handleConfigs.Clear();
			_handleConfigs = null;
		}
	}

	/// <summary>
	/// Add a handle that the user can drag horizontally or vertically.
	/// This should only be called in <see cref="OnConfigureHandles"/>.
	/// </summary>
	protected void AddHandle( ConnectionHandleConfig config )
	{
		HandleConfigs.Add( config );
	}

	protected void ResetHandle( Connection connection, string name )
	{
		connection.Input?.Inner.SetHandleOffset( name, null );
	}

	/// <summary>
	/// Called at the start of <see cref="Layout"/>. A good time to set <see cref="Connection.StyleData"/>.
	/// </summary>
	/// <param name="connection">Connection to perform a layout for.</param>
	/// <param name="sceneStart">Connection start position in the scene.</param>
	/// <param name="sceneEnd">Connection end position in the scene.</param>
	protected virtual void OnPreLayout( Connection connection, Vector2 sceneStart, Vector2 sceneEnd ) { }

	/// <summary>
	/// Call <see cref="AddHandle"/> here.
	/// </summary>
	/// <param name="connection">Connection to get the bounds for.</param>
	/// <param name="sceneStart">Connection start position in the scene.</param>
	/// <param name="sceneEnd">Connection end position in the scene.</param>
	protected virtual void OnConfigureHandles( Connection connection, Vector2 sceneStart, Vector2 sceneEnd ) { }

	/// <summary>
	/// When overridden, gets a rectangle in scene space that fully encloses the line of
	/// a given connection.
	/// </summary>
	/// <param name="connection">Connection to get the bounds for.</param>
	/// <param name="sceneStart">Connection start position in the scene.</param>
	/// <param name="sceneEnd">Connection end position in the scene.</param>
	protected abstract Rect OnGetBounds( Connection connection, Vector2 sceneStart, Vector2 sceneEnd );

	/// <summary>
	/// When overridden, calls methods like <see cref="GraphicsLine.MoveTo"/> and <see cref="GraphicsLine.LineTo"/>
	/// on the given connection to describe its path in local space.
	/// </summary>
	/// <param name="connection">Connection to perform a layout for.</param>
	/// <param name="sceneStart">Connection start position in the scene.</param>
	/// <param name="sceneEnd">Connection end position in the scene.</param>
	protected abstract void OnLayout( Connection connection, Vector2 sceneStart, Vector2 sceneEnd );

	protected static float SnapToGrid( float value, float gridSize )
	{
		return MathF.Floor( value / gridSize ) * gridSize;
	}

	protected static Vector2 SnapToGrid( Vector2 pos, float gridSize )
	{
		return new Vector2( SnapToGrid( pos.x, gridSize ), SnapToGrid( pos.y, gridSize ) );
	}

	protected static Rect SnapToGrid( Rect rect, float gridSize )
	{
		var min = SnapToGrid( rect.Position, gridSize );
		var max = SnapToGrid( rect.Position + rect.Size, gridSize );

		return new Rect( min, max - min );
	}
}

/// <summary>
/// Original curvy cubic line style.
/// </summary>
public sealed class ClassicConnectionStyle : ConnectionStyle
{
	protected override Rect OnGetBounds( Connection connection, Vector2 sceneStart, Vector2 sceneEnd )
	{
		return new Rect( sceneStart ).AddPoint( sceneEnd ).Grow( 64f );
	}

	protected override void OnLayout( Connection connection, Vector2 sceneStart, Vector2 sceneEnd )
	{
		var localStart = connection.FromScene( sceneStart );
		var localEnd = connection.FromScene( sceneEnd );

		var dir = localEnd - localStart;
		var length = dir.Length;
		dir = dir.Normal;

		var ease = Sandbox.Utility.Easing.ExpoOut( 1.0f - System.MathF.Abs( Vector2.Dot( dir, Vector2.Up ) ) );
		var dist = (64 * ease).Clamp( 0, length * 0.5f );
		var legde = new Vector2( (16 * ease).Clamp( 0, (length * 0.5f) - 16 ), 0 );

		localEnd -= legde;
		connection.MoveTo( localStart );
		localStart += legde;
		connection.LineTo( localStart );
		connection.CubicLineTo( localStart + new Vector2( dist, 0 ), localEnd + new Vector2( -dist, 0 ), localEnd );
		localEnd += legde;
		connection.LineTo( localEnd );
	}
}

public sealed class GridConnectionStyle : ConnectionStyle
{
	public static GridConnectionStyle Instance { get; } = new();

	private enum PathKind
	{
		Straight,
		TwoCorners,
		FourCorners
	}

	private class StyleData
	{
		public float GridSize { get; set; }
		public Vector2 SceneStart { get; set; }
		public Vector2 SceneEnd { get; set; }
		public Rect StartRect { get; set; }
		public Rect EndRect { get; set; }
		public PathKind PathKind { get; set; }

		public float A { get; set; }
		public float B { get; set; }
		public float Y { get; set; }
	}

	private static float GetGridSize( Connection connection ) => (connection.GraphicsView as GraphView)?.GridSize ?? 12f;

	protected override void OnPreLayout( Connection connection, Vector2 sceneStart, Vector2 sceneEnd )
	{
		if ( connection.StyleData is not StyleData data )
		{
			connection.StyleData = data = new StyleData();
		}

		var gridSize = GetGridSize( connection );

		sceneStart = SnapToGrid( sceneStart, gridSize );
		sceneEnd = SnapToGrid( sceneEnd, gridSize );

		var startRect = new Rect( sceneStart );
		var endRect = new Rect( sceneEnd );

		if ( connection.Output?.Node is { } startNode )
		{
			startRect = SnapToGrid( startNode.SceneRect, gridSize );
		}

		if ( connection.Input?.Node is { } endNode )
		{
			endRect = SnapToGrid( endNode.SceneRect, gridSize );
		}

		startRect = startRect.Grow( gridSize );
		endRect = endRect.Grow( gridSize );

		data.GridSize = gridSize;
		data.SceneStart = sceneStart;
		data.SceneEnd = sceneEnd;
		data.StartRect = startRect;
		data.EndRect = endRect;

		data.PathKind = startRect.Right <= endRect.Left
			? sceneStart.y.AlmostEqual( sceneEnd.y ) ? PathKind.Straight : PathKind.TwoCorners
			: PathKind.FourCorners;
	}

	protected override void OnConfigureHandles( Connection connection, Vector2 sceneStart, Vector2 sceneEnd )
	{
		var data = (StyleData)connection.StyleData;

		if ( data.PathKind == PathKind.Straight )
		{
			//
			// [start]-------[end]
			//

			data.A = 0f;
			data.B = 0f;
			data.Y = 0f;

			ResetHandle( connection, "x" );
			ResetHandle( connection, "y" );
			ResetHandle( connection, "a" );
			ResetHandle( connection, "b" );

			return;
		}

		var hintStart = GetPlugIndex( connection.Output );
		var hintStartOffset = hintStart.Index * data.GridSize;

		if ( data.PathKind == PathKind.TwoCorners )
		{
			//
			// [start]-+
			//         |
			//         A
			//         |
			//         +---[end]
			//

			var config = new ConnectionHandleConfig( "x",
				DragDirection.Horizontal, ConnectionPlug.Output,
				new Vector2( data.StartRect.Right, (data.SceneStart.y + data.SceneEnd.y) * 0.5f ),
				hintStartOffset,
				Min: 0f, Max: data.EndRect.Left - data.StartRect.Right );

			AddHandle( config );

			ResetHandle( connection, "y" );
			ResetHandle( connection, "a" );
			ResetHandle( connection, "b" );

			data.A = data.B = data.StartRect.Right + config.GetValue( connection );
			data.Y = data.SceneStart.y;
			return;
		}

		//
		// [start]---+
		//           A
		//   +---Y---+
		//   B
		//   +--[end]
		//

		var hintMid = GetConnectionIndex( connection );
		var hintEnd = GetPlugIndex( connection.Input );

		var hintEndOffset = (hintEnd.Count - hintEnd.Index - 1) * data.GridSize;
		var hintMidOffset = hintMid.Index * data.GridSize;

		var startEdgeY = Math.Clamp( sceneEnd.y, data.StartRect.Top - (hintMid.Count - 1) * data.GridSize, data.StartRect.Bottom );

		var aConfig = new ConnectionHandleConfig( "a",
			DragDirection.Horizontal, ConnectionPlug.Output,
			new Vector2( data.StartRect.Right, default ),
			hintStartOffset,
			Min: 0f );

		var bConfig = new ConnectionHandleConfig( "b",
			DragDirection.Horizontal, ConnectionPlug.Input,
			new Vector2( data.EndRect.Left, default ),
			-hintEndOffset,
			Max: 0f );

		var yConfig = new ConnectionHandleConfig( "y",
			DragDirection.Vertical, ConnectionPlug.Output,
			new Vector2( default, sceneStart.y ),
			startEdgeY + hintMidOffset - sceneStart.y );

		var a = data.StartRect.Right + aConfig.GetValue( connection );
		var b = data.EndRect.Left + bConfig.GetValue( connection );
		var y = sceneStart.y + yConfig.GetValue( connection );

		data.A = a;
		data.B = b;
		data.Y = y;

		AddHandle( aConfig with { SceneOrigin = new Vector2( data.StartRect.Right, (data.SceneStart.y + y) * 0.5f ) } );
		AddHandle( bConfig with { SceneOrigin = new Vector2( data.EndRect.Left, (data.SceneEnd.y + y) * 0.5f ) } );
		AddHandle( yConfig with { SceneOrigin = new Vector2( (a + b) * 0.5f, sceneStart.y ) } );

		ResetHandle( connection, "x" );
	}

	private static (int Index, int Count) FindIndexAndCount<T>( IEnumerable<T> items, Predicate<T> predicate )
	{
		var count = 0;
		var index = -1;

		foreach ( var item in items )
		{
			if ( index == -1 && predicate( item ) )
			{
				index = count;
			}

			++count;
		}

		return (index, count);
	}

	/// <summary>
	/// Count how many connected plugs are above this output / below this
	/// input on the same node.
	/// </summary>
	private static (int Index, int Count) GetPlugIndex( Plug plug )
	{
		if ( plug is null ) return (0, 1);

		var plugs = plug is PlugIn
			? plug.Node.Inputs.Cast<Plug>()
			: plug.Node.Outputs;

		return FindIndexAndCount( plugs.Where( x => x.IsConnected ), x => x == plug );
	}

	/// <summary>
	/// Count how many connections are above this one between the same two nodes.
	/// </summary>
	private static (int Index, int Count) GetConnectionIndex( Connection connection )
	{
		if ( connection.Output?.Node is not { } outNode ) return (0, 1);
		if ( connection.Input?.Node is not { } inNode ) return (0, 1);

		return FindIndexAndCount( outNode.Outputs.Where( x => x.Connections.Any( y => y.Input?.Node == inNode ) ),
			x => x == connection.Output );
	}

	protected override Rect OnGetBounds( Connection connection, Vector2 sceneStart, Vector2 sceneEnd )
	{
		var data = (StyleData)connection.StyleData;

		return (data.PathKind switch
		{
			PathKind.FourCorners => Rect.FromPoints( sceneStart, sceneEnd )
				.AddPoint( new Vector2( data.A, data.Y ) )
				.AddPoint( new Vector2( data.B, data.Y ) ),
			_ => Rect.FromPoints( sceneStart, sceneEnd )
		}).Grow( 16f );
	}

	protected override void OnLayout( Connection connection, Vector2 sceneStart, Vector2 sceneEnd )
	{
		var data = (StyleData)connection.StyleData;

		var gridSize = data.GridSize;

		var localStart = connection.FromScene( sceneStart );
		var localEnd = connection.FromScene( sceneEnd );

		var a = new Vector2( data.A, data.Y );
		var b = new Vector2( data.B, data.Y );

		var localA = connection.FromScene( a );
		var localB = connection.FromScene( b );

		var prev = localStart;
		var curr = localStart;

		connection.MoveTo( localStart );

		switch ( data.PathKind )
		{
			case PathKind.TwoCorners:
				CurvedLineTo( connection, gridSize, ref prev, ref curr, localStart.WithX( localA.x ) );
				CurvedLineTo( connection, gridSize, ref prev, ref curr, localEnd.WithX( localB.x ) );
				CurvedLineTo( connection, gridSize, ref prev, ref curr, localEnd );
				break;

			case PathKind.FourCorners:
				CurvedLineTo( connection, gridSize, ref prev, ref curr, localStart.WithX( localA.x ) );
				CurvedLineTo( connection, gridSize, ref prev, ref curr, localA );
				CurvedLineTo( connection, gridSize, ref prev, ref curr, localB );
				CurvedLineTo( connection, gridSize, ref prev, ref curr, localEnd.WithX( localB.x ) );
				CurvedLineTo( connection, gridSize, ref prev, ref curr, localEnd );
				break;
		}

		connection.LineTo( localEnd );
	}

	private static void CurvedLineTo( Connection connection, float gridSize, ref Vector2 prev, ref Vector2 curr, Vector2 next )
	{
		if ( next.AlmostEqual( curr ) )
		{
			return;
		}

		var prevTangent = curr.AlmostEqual( prev )
			? new Vector2( 1f, 0f )
			: (curr - prev).Normal;

		var nextTangent = (next - curr).Normal;

		if ( nextTangent.AlmostEqual( -prevTangent ) )
		{
			curr = next;
			return;
		}

		if ( nextTangent.AlmostEqual( prevTangent ) )
		{
			curr = next;
			return;
		}

		var radius = Math.Min( gridSize, (next - curr).Length ) * 0.5f;

		var a = curr - prevTangent * radius;
		var b = curr + nextTangent * radius;

		connection.LineTo( a );
		connection.CubicLineTo( a + prevTangent * radius * 0.5f, b - nextTangent * radius * 0.5f, b );

		prev = curr;
		curr = next;
	}
}
