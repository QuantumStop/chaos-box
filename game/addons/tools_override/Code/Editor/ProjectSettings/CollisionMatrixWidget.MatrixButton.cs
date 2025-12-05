using Sandbox.Physics;
namespace Editor.ProjectSettingPages;

public partial class CollisionMatrixWidget
{
	public class MatrixButton : Widget
	{
		CollisionMatrixWidget Matrix;
		public string Left { get; set; }
		public string Right { get; set; }

		public MatrixButton( CollisionMatrixWidget parent, string left, string right ) : base( parent )
		{
			Matrix = parent;
			Left = left;
			Right = right;
			Cursor = CursorShape.Finger;
		}

		protected override void OnPaint()
		{
			base.OnPaint();

			var rule = CurrentValue;

			Paint.ClearPen();
			Paint.Antialiasing = false;
			var r = LocalRect.Shrink( 0, 0, 2, 2 );

			if ( rule == CollisionRules.Result.Unset )
			{
				Paint.SetBrush( Color.White.WithAlpha( Paint.HasMouseOver ? 0.2f : 0.05f ) );
				Paint.ClearPen();
				Paint.DrawRect( r, 2 );
			}

			if ( rule == CollisionRules.Result.Ignore )
			{
				Paint.SetBrush( Theme.Red.WithAlpha( Paint.HasMouseOver ? 0.4f : 0.3f ) );
				Paint.ClearPen();
				Paint.DrawRect( r, 2 );

				Paint.SetPen( Theme.Red );
				Paint.DrawIcon( r, "clear", 14 );
			}

			if ( rule == CollisionRules.Result.Trigger )
			{
				Paint.SetBrush( Theme.Yellow.Desaturate( 0.4f ).Darken( Paint.HasMouseOver ? 0.4f : 0.6f ) );
				Paint.ClearPen();
				Paint.DrawRect( r, 2 );

				Paint.SetPen( Theme.Yellow );
				Paint.DrawIcon( r, "remove", 14 );
			}

			if ( rule == CollisionRules.Result.Collide )
			{
				Paint.SetBrush( Theme.Green.Darken( Paint.HasMouseOver ? 0.4f : 0.5f ) );
				Paint.ClearPen();
				Paint.DrawRect( r, 2 );

				Paint.SetPen( Theme.Green );
				Paint.DrawIcon( r, "check", 14 );
			}
		}

		CollisionRules.Result CurrentValue
		{
			get
			{
				if ( Right != null )
					return Matrix.FindPair( Left, Right );

				if ( Matrix.current.Defaults.TryGetValue( Left, out var rule ) )
					return rule;

				return CollisionRules.Result.Unset;
			}
		}

		protected override void OnMousePress( MouseEvent e )
		{
			e.Accepted = true;

			var rule = CurrentValue;
			if ( e.RightMouseButton )
			{
				rule = 0;
			}
			else
			{
				if ( rule >= CollisionRules.Result.Ignore ) rule = 0;
				else rule++;
			}

			// power users
			if ( e.HasShift ) rule = CollisionRules.Result.Collide;
			if ( e.HasCtrl ) rule = CollisionRules.Result.Ignore;
			if ( e.HasAlt ) rule = CollisionRules.Result.Trigger;
			if ( e.HasAlt && e.HasShift ) rule = CollisionRules.Result.Unset;

			Matrix.SetPair( Left, Right, rule );
		}

		protected override void OnMouseEnter()
		{
			base.OnMouseEnter();

			Matrix.OnHovered( this );
		}

		protected override void OnMouseLeave()
		{
			base.OnMouseLeave();

			Matrix.OnHovered( null );
		}
	}
}
