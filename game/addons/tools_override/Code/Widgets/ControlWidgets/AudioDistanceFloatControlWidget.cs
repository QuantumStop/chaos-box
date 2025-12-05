namespace Editor;

using Sandbox.Audio;

[CustomEditor( typeof( float ), WithAllAttributes = new Type[] { typeof( AudioDistanceFloatAttribute ) } )]
public class AudioDistanceFloatControlWidget : ControlObjectWidget
{
	public override bool SupportsMultiEdit => true;

	public AudioDistanceFloatControlWidget( SerializedProperty property ) : base( property, true )
	{
		Layout = Layout.Row();

		var w = Layout.Add( new FloatControlWidget( property ) );
		w.MakeRanged( new Vector2( 1, 50_000 ), 1, true, true );
		w.Icon = "radar";
		w.Label = null;

		Layout.Add( new PresetsWidget( this, property ) );
	}
}

file class PresetsWidget : Widget
{
	private readonly SerializedProperty _property;

	public PresetsWidget( Widget parent, SerializedProperty property ) : base( parent )
	{
		FixedWidth = 100;

		_property = property;
	}

	public void EditValue( float value )
	{
		_property.SetValue( value );
	}

	protected override void OnPaint()
	{
		Paint.Antialiasing = true;

		Paint.ClearPen();

		Paint.SetBrush( Theme.ControlBackground );
		Paint.DrawRect( LocalRect, Theme.ControlRadius );

		var value = _property.As.Float;

		var rect = LocalRect;
		rect.Left = 8;

		Paint.SetPen( Color.Lerp( Theme.Green, Theme.Red, value / 50_000f ) );
		Paint.SetDefaultFont();
		Paint.DrawText( rect, $"{DistanceString( value )}", TextFlag.LeftCenter );
	}

	protected override void OnMouseClick( MouseEvent e )
	{
		base.OnMouseClick( e );

		if ( e.LeftMouseButton )
		{
			var m = new Menu( this );

			var value = _property.As.Float;

			foreach ( var db in DistanceNames )
			{
				var o = m.AddOption( $"{db.Distance} units - {db.Name}", null, () => EditValue( db.Distance ) );
				o.Checkable = true;
				o.Checked = value == db.Distance;
			}

			m.OpenAt( e.ScreenPosition );
		}
	}

	public static List<(float Distance, string Name)> DistanceNames = new()
	{
		( 500, "leaf rustle" ),
		( 750, "whispering" ),
		( 1_000, "library" ),
		( 1_500, "fridge" ),
		( 2_000, "avg home" ),
		( 3_500, "clothes dryer" ),
		( 5_000, "dishwasher" ),
		( 7_000, "vacuum cleaner" ),
		( 8_500, "traffic" ),
		( 10_000, "restaurant" ),
		( 11_000, "alarm clock" ),
		( 12_500, "child scream" ),
		( 13_000, "motorcycle" ),
		( 14_000, "subway train" ),
		( 15_000, "helicopter" ),
		( 17_500, "sandblasting" ),
		( 20_000, "jet plane" ),
		( 25_000, "air raid siren" ),
		( 30_000, "gunshot" ),
		( 32_500, "fireworks" ),
		( 35_000, "jet engine" ),
		( 40_000, "shotgun" ),
		( 42_500, "357 revolver" ),
		( 45_000, "grenade" ),
		( 50_000, "rocket launch" )
	};

	public static string DistanceString( float value )
	{
		foreach ( var f in DistanceNames )
		{
			if ( value <= f.Distance ) return f.Name;
		}

		return "really loud";
	}
}
