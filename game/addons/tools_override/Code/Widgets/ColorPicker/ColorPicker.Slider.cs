
using System;

namespace Editor;

public partial class ColorPicker
{
	class Slider : Widget
	{
		public Vector2 Value { get; set; }

		protected ColorPicker Picker => Parent as ColorPicker;
		protected virtual bool Xy => true;

		private bool IsGrabbed;

		public event Action EditingFinished;

		public event Action EditingStarted;

		/// <summary>
		/// If true we'll add an artificial margin so the handle can't go outside
		/// </summary>
		public bool HandleMargin { get; set; } = true;

		/// <summary>
		/// The size of the handle circle
		/// </summary>
		public float HandleSize = 18f;
		private float handleSizeHalf => HandleSize * 0.5f;

		public Slider( ColorPicker parent ) : base( parent )
		{
			MinimumSize = HandleSize;
			FocusMode = FocusMode.Click;
		}

		protected override void OnPaint()
		{
			var rect = BackgroundRect();

			PaintBackground( rect );

			var y = Xy ? Value.y : 0.5f;
			var center = new Vector2( Value.x * rect.Width + rect.Left, y * rect.Height + rect.Top ) - handleSizeHalf + 1;
			var handleRect = new Rect( center, HandleSize );

			PaintHandle( handleRect );
		}

		protected override void OnMousePress( MouseEvent e )
		{
			base.OnMousePress( e );

			EditingStarted?.Invoke();

			IsGrabbed = true;

			SetValueFromLocalPosition( e.LocalPosition );
		}

		protected override void OnMouseReleased( MouseEvent e )
		{
			base.OnMouseReleased( e );

			IsGrabbed = false;

			EditingFinished?.Invoke();
		}

		protected override void OnMouseMove( MouseEvent e )
		{
			base.OnMouseMove( e );

			SetValueFromLocalPosition( e.LocalPosition );

			Picker.Update();
		}

		protected virtual void PaintBackground( Rect rect )
		{
			var color = new ColorHsv( Picker.Hsv.Hue, 1, 1 );

			Paint.ClearPen();
			Paint.SetBrushLinear( 0, rect.TopRight, Color.White, color );
			Paint.DrawRect( rect, 4 );
			Paint.SetBrushLinear( 0, rect.BottomLeft, Color.Transparent, Color.Black );
			Paint.DrawRect( rect, 4 );
		}

		protected virtual void PaintHandle( Rect rect )
		{
			var circleThickness = rect.Height * .20f;
			var circleSize = rect.Height - circleThickness;
			var circlePosition = rect.Center;
			var circleColor = Color.White;

			if ( IsGrabbed )
			{
				circleColor = Theme.Green;
				circleThickness += 1.0f;
				circleSize -= 2.0f;
			}

			Paint.Antialiasing = true;

			Paint.SetBrushRadial( circlePosition, circleSize * .8f, Color.Black.WithAlpha( .72f ), Color.Transparent );
			Paint.ClearPen();
			Paint.DrawCircle( circlePosition + new Vector2( 3f, 4f ), circleSize + 4f );

			Paint.SetPen( circleColor, circleThickness );
			Paint.SetBrush( "/image/transparent-small.png" );
			Paint.DrawCircle( circlePosition, circleSize );
			Paint.SetBrush( Picker.Value );
			Paint.DrawCircle( circlePosition, circleSize );
		}

		private void SetValueFromLocalPosition( Vector2 localPosition )
		{
			var rect = BackgroundRect();
			var localPos = localPosition - rect.TopLeft;

			var x = (localPos.x / rect.Width).Clamp( 0, 1 );
			var y = (localPos.y / rect.Height).Clamp( 0, 1 );

			if ( Xy )
			{
				Value = new Vector2( x, y );
			}
			else
			{
				Value = Value.WithX( x );
			}

			SignalValuesChanged();
		}

		private Rect BackgroundRect()
		{
			if ( !HandleMargin )
			{
				return ContentRect;
			}

			if ( Height == MinimumHeight )
			{
				return ContentRect.Shrink( handleSizeHalf, handleSizeHalf );
			}

			return ContentRect.Shrink( handleSizeHalf );
		}

	}
}
