namespace Editor;

public partial class SoundPlayer
{
	public class TimeAxis : GraphicsItem
	{
		private readonly TimelineView TimelineView;

		public TimeAxis( TimelineView view )
		{
			TimelineView = view;
			ZIndex = -1;
			HoverEvents = true;
		}

		protected override void OnMousePressed( GraphicsMouseEvent e )
		{
			base.OnMousePressed( e );

			if ( e.LeftMouseButton )
			{
				TimelineView.MoveScrubber( e.LocalPosition.x );
			}
		}

		protected override void OnPaint()
		{
			base.OnPaint();

			Paint.Antialiasing = false;
			Paint.ClearPen();
			Paint.SetBrush( Theme.ControlBackground );
			Paint.DrawRect( LocalRect );

			Paint.SetDefaultFont( 7 );

			var rect = LocalRect.Shrink( 1 );
			int visibleWidth = (int)TimelineView.VisibleRect.Width;

			float visibleDuration = TimelineView.TimeFromPosition( visibleWidth );

			const int targetMajorPixelSpacing = 100;

			float majorStepDuration = visibleDuration * (targetMajorPixelSpacing / (float)visibleWidth);
			majorStepDuration = GetNiceStepSize( majorStepDuration );

			const int subdivisions = 10;
			float minorStepDuration = majorStepDuration / subdivisions;

			// Calculate the starting and ending step indices based on the visible range
			int startStep = (int)Math.Floor( TimelineView.TimeFromPosition( TimelineView.VisibleRect.Left ) / majorStepDuration );
			int endStep = (int)Math.Ceiling( TimelineView.TimeFromPosition( TimelineView.VisibleRect.Right ) / majorStepDuration );

			for ( int step = startStep; step <= endStep; step++ )
			{
				float majorLineTime = step * majorStepDuration;
				float majorLineX = rect.Left + majorLineTime * (visibleWidth / visibleDuration);

				// major line
				Paint.SetPen( Theme.Text.WithAlpha( 0.5f ) );
				Paint.DrawLine( new Vector2( majorLineX, rect.Bottom ), new Vector2( majorLineX, rect.Bottom - 8 ) );

				// label
				if ( Math.Abs( majorLineTime ) > 0 )
				{
					string label = majorLineTime < 60 ? $"{majorLineTime}" : $"{(int)(majorLineTime / 60)}:{(majorLineTime % 60):00}";
					var textRect = new Rect( new Vector2( majorLineX, rect.Top ), Paint.MeasureText( label ) );
					textRect.Left -= textRect.Width;
					Paint.DrawText( textRect, label, TextFlag.CenterHorizontally );
				}

				// minor subdivisions
				Paint.SetPen( Theme.Text.WithAlpha( 0.2f ) );
				for ( int j = 1; j < subdivisions; j++ )
				{
					float minorLineTime = majorLineTime + j * minorStepDuration;
					float minorLineX = rect.Left + minorLineTime * (visibleWidth / visibleDuration);

					Paint.DrawLine( new Vector2( minorLineX, rect.Bottom ), new Vector2( minorLineX, rect.Bottom - 4 ) );
				}
			}
		}

		/// <summary>
		/// Rounds a step size to a "nice" value (e.g., 1, 2, 5, 10, etc.) while maintaining integer-based accuracy.
		/// </summary>
		private float GetNiceStepSize( float step )
		{
			float[] niceSteps = { 1f, 2f, 5f, 10f, 20f, 50f, 100f, 200f, 500f, 1000f };
			foreach ( var niceStep in niceSteps )
			{
				if ( step <= niceStep )
					return niceStep;
			}
			return niceSteps[niceSteps.Length - 1]; // Return the largest step if none match
		}

	}
}
