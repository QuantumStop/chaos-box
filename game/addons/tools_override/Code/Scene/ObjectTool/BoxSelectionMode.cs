namespace Editor;

class BoxSelectionMode : SceneSelectionMode
{
	public BoxSelectionMode( Scene scene, SelectionSystem selection ) : base( scene, selection )
	{
	}

	HashSet<object> selected = new();
	bool removing = false;

	public override void OnMouseMoved()
	{
		// project to screen position
		var c1 = Gizmo.Camera.ToScreen( FirstRay.Project( 100 ) );
		var c2 = Gizmo.Camera.ToScreen( Ray.Project( 100 ) );

		if ( Vector2.Distance( c1, c2 ) < 5 )
			return;

		var frustum = Gizmo.Camera.GetFrustum( Rect.FromPoints( c1, c2 ) );

		var selection = new HashSet<GameObject>();
		var previous = new HashSet<GameObject>();

		bool fullyInside = true;

		foreach ( var mr in Scene.GetAllComponents<ModelRenderer>() )
		{
			var bounds = mr.Bounds;
			if ( !frustum.IsInside( bounds, !fullyInside ) )
			{
				previous.Add( mr.GameObject );
				continue;
			}

			selection.Add( mr.GameObject );
		}

		foreach ( var go in Scene.GetAllObjects( true ) )
		{
			if ( selection.Contains( go ) ) continue;
			if ( !go.HasGizmoHandle ) continue;
			if ( !frustum.IsInside( go.WorldPosition ) )
			{
				previous.Add( go );
				continue;
			}

			selection.Add( go );
		}

		foreach ( var selectedObj in selection )
		{
			if ( !removing )
			{
				if ( selected.Contains( selectedObj ) ) continue;
				if ( Selection.Contains( selectedObj ) ) continue;

				Selection.Add( selectedObj );
			}
			else
			{
				if ( !Selection.Contains( selectedObj ) ) continue;

				Selection.Remove( selectedObj );
			}
		}

		foreach ( var removed in previous )
		{
			if ( removing )
			{
				if ( !selected.Contains( removed ) ) continue;

				Selection.Add( removed );
			}
			else
			{
				if ( selected.Contains( removed ) ) continue;

				Selection.Remove( removed );
			}
		}
	}

	public override void OnUpdate()
	{
		//	Gizmo.Draw.Color = Theme.Blue;
		//	foreach ( var e in selected )
		//	{
		//		Gizmo.Draw.LineBBox( e.GetBounds().Grow( 0.2f ) );
		//	}

		var a = FirstRay.Project( 100 );
		var b = Ray.Project( 100 );

		var sa = Gizmo.Camera.ToScreen( a );
		var sb = Gizmo.Camera.ToScreen( b );

		Rect rect = new Rect( MathF.Min( sa.x, sb.x ), MathF.Min( sa.y, sb.y ), MathF.Abs( sa.x - sb.x ), MathF.Abs( sa.y - sb.y ) );

		Gizmo.Draw.ScreenRect( rect, Theme.Blue.WithAlpha( 0.1f ), new Vector4( 4.0f ), Theme.Blue, new Vector4( 4.0f ) );
	}

	IDisposable _selectionUndoScope = null;

	public override void OnEnabled()
	{
		removing = Gizmo.IsCtrlPressed;
		if ( removing || Gizmo.IsShiftPressed )
		{
			selected = Selection.ToHashSet();
		}
		_selectionUndoScope = SceneEditorSession.Active.UndoScope( "Box Select Object(s)" ).Push();
	}

	public override void OnDisabled()
	{
		_selectionUndoScope?.Dispose();
		_selectionUndoScope = null;
	}
}
