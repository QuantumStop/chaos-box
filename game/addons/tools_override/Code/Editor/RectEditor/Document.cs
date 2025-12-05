using System.Text.Json.Serialization;

namespace Editor.RectEditor;

public enum SelectionOperation
{
	None,
	Set,
	Add,
	Remove,
};

public class Document
{
	public class Rectangle
	{
		[ShowIf( nameof( UseNormalizedValues ), true )]
		public Vector2 Min { get; set; }

		[ShowIf( nameof( UseNormalizedValues ), true )]
		public Vector2 Max { get; set; }

		[JsonIgnore, Step( 1 )]
		[ShowIf( nameof( UseNormalizedValues ), false )]
		public Vector2 Position
		{
			get => Min * Session.GetImageSize();
			set
			{
				var lastMin = Min;
				Min = value / Session.GetImageSize();
				Max += (Min - lastMin);
			}
		}

		[JsonIgnore, Step( 1 )]
		[ShowIf( nameof( UseNormalizedValues ), false )]
		public Vector2 Size
		{
			get => (Max - Min) * Session.GetImageSize();
			set
			{
				Max = Min + value / Session.GetImageSize();
			}
		}

		public bool AllowRotation { get; set; }
		public bool AllowTiling { get; set; }
		public Color Color { get; set; }

		[Hide, JsonIgnore] Window Session;
		[Hide, JsonIgnore] bool UseNormalizedValues => Session?.Settings?.ShowNormalizedValues ?? false;

		public bool IsPointInRectangle( Vector2 point )
		{
			return (point.x >= Min.x) && (point.x <= Max.x) &&
				(point.y >= Min.y) && (point.y <= Max.y);
		}

		public float DistanceFromPointToCenter( Vector2 point )
		{
			return point.Distance( (Min + Max) * 0.5f );
		}

		public Rectangle( Window window )
		{
			Session = window;
		}
	};

	public List<Rectangle> Rectangles { get; set; } = new();
	public List<Rectangle> SelectedRectangles { get; set; } = new();

	public bool Modified { get; set; }

	[JsonIgnore]
	public Action OnModified { get; set; }

	Window Session;

	public Document()
	{
	}

	public Document( Window session, RectAssetData data, Action onModified )
	{
		OnModified = onModified;
		Rectangles = new List<Rectangle>();
		Session = session;

		var rectangles = data.RectangleSets?.FirstOrDefault()?.Rectangles;
		if ( rectangles is null )
			return;

		foreach ( var rectangle in rectangles )
		{
			Rectangles.Add( new Rectangle( Session )
			{
				Min = new Vector2( (float)rectangle.Min[0] / 32768, (float)rectangle.Min[1] / 32768 ),
				Max = new Vector2( (float)rectangle.Max[0] / 32768, (float)rectangle.Max[1] / 32768 ),
				AllowRotation = rectangle.Properties is not null && rectangle.Properties.AllowRotation,
				AllowTiling = rectangle.Properties is not null && rectangle.Properties.AllowTiling,
				Color = RandomColor()
			} );
		}
	}

	public Rectangle AddRectangle( Window session, Rect rect )
	{
		var rectangle = new Rectangle( session )
		{
			Min = rect.TopLeft,
			Max = rect.BottomRight,
			Color = RandomColor()
		};

		Rectangles.Add( rectangle );

		SetModified();

		return rectangle;
	}

	public void DeleteRectangles( IEnumerable<Rectangle> rectangles )
	{
		foreach ( var rectangle in rectangles )
		{
			SelectedRectangles.Remove( rectangle );
			Rectangles.Remove( rectangle );
		}

		SetModified();
	}

	public void ClearSelection()
	{
		SelectedRectangles.Clear();

		OnModified?.Invoke();
	}

	public void SelectAll()
	{
		SelectedRectangles = Rectangles.ToList();

		OnModified?.Invoke();
	}

	public void SelectRectangle( Rectangle rectangle, SelectionOperation op )
	{
		if ( op == SelectionOperation.Set )
		{
			SelectedRectangles.Clear();

			if ( rectangle is null )
			{
				op = SelectionOperation.None;
			}
			else
			{
				op = SelectionOperation.Add;
			}
		}

		if ( op == SelectionOperation.Add )
		{
			SelectedRectangles.Add( rectangle );
		}
		else if ( op == SelectionOperation.Remove )
		{
			SelectedRectangles.Remove( rectangle );
		}

		OnModified?.Invoke();
	}

	public bool IsRectangleSelected( Rectangle rectangle )
	{
		return SelectedRectangles.Contains( rectangle );
	}

	private void SetModified()
	{
		Modified = true;
		OnModified?.Invoke();
	}

	private static Color RandomColor()
	{
		const int rangeMin = 32;
		const int rangeMax = 128;

		var r = (byte)Game.Random.Int( rangeMin, rangeMax );
		var g = (byte)Game.Random.Int( rangeMin, rangeMax );
		var b = (byte)Game.Random.Int( rangeMin, rangeMax );
		return new Color32( r, g, b ).ToColor();
	}
}
