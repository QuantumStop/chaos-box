namespace Editor.Internal;

internal class GamePerformanceBar : Widget
{
	System.Func<string> GetValue;

	public GamePerformanceBar( System.Func<string> val ) : base( null )
	{
		GetValue = val;
		MinimumHeight = Theme.RowHeight;
		MinimumWidth = 60;
	}

	protected override void DoLayout()
	{
		base.DoLayout();


	}

	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.ClearPen();
		Paint.SetBrush( Theme.ControlBackground );
		Paint.DrawRect( LocalRect, Theme.ControlRadius );

		Paint.SetPen( Theme.Text );
		Paint.DrawText( LocalRect.Shrink( 8, 0 ), GetValue(), TextFlag.RightCenter );
	}

	RealTimeSince timeSinceUpdate;

	[EditorEvent.Frame]
	public void Frame()
	{
		if ( timeSinceUpdate < 0.6f )
			return;

		timeSinceUpdate = System.Random.Shared.Float( 0, 0.1f );

		Update();
	}
}

