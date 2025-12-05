namespace Editor;

/// <summary>
/// Allows easy placement of a control relative to another control, in screen space. Most useful for 
/// when you pop up a menu or something and want it positioned relative to another control.
/// </summary>
public struct WidgetAnchor
{
	public enum PositionModes
	{
		BottomStart, BottomEnd, TopStart, TopEnd,
		LeftStart, LeftEnd, RightStart, RightEnd
	}

	/// <summary>
	/// The position
	/// </summary>
	public PositionModes Position { get; set; }

	/// <summary>
	/// Pixel offset
	/// </summary>
	public float Offset { get; set; }

	/// <summary>
	/// Keep the child control on the screen
	/// </summary>
	public bool ConstrainToScreen { get; set; }

	/// <summary>
	/// Adjust the size of the child control to its optimal size before placing it
	/// </summary>
	public bool AdjustSize { get; set; }

	// TODO - pinned, where we update every frame
	// TODO - FollowMinSize, where the child control is minimim the size of the parent


	/// <summary>
	/// Align with the top left of our control at the bottom left of the parent control
	/// </summary>
	public static WidgetAnchor BottomStart => new WidgetAnchor { Position = PositionModes.BottomStart };

	/// <summary>
	/// Align with the top right of our control at the bottom right of the parent control
	/// </summary>
	public static WidgetAnchor BottomEnd => new WidgetAnchor { Position = PositionModes.BottomEnd };

	/// <summary>
	/// Align with the top right of our control at the top left of the parent control
	/// </summary>
	public static WidgetAnchor LeftStart => new WidgetAnchor { Position = PositionModes.LeftStart };


	public void Apply( Widget child, Widget parent )
	{
		if ( child == null || parent == null ) return;

		if ( AdjustSize )
		{
			child.AdjustSize();
		}

		var parentBox = parent.ScreenRect;
		var childSize = child.ScreenRect;
		Vector2 localPos = Vector2.Zero;

		if ( Position == PositionModes.BottomStart )
		{
			localPos.x = parentBox.Left;
			localPos.y = parentBox.Bottom + Offset;
		}
		else if ( Position == PositionModes.BottomEnd )
		{
			localPos.x = parentBox.Right - childSize.Width;
			localPos.y = parentBox.Bottom + Offset;
		}
		else if ( Position == PositionModes.TopStart )
		{
			localPos.x = parentBox.Left;
			localPos.y = parentBox.Top - childSize.Height - Offset;
		}
		else if ( Position == PositionModes.TopEnd )
		{
			localPos.x = parentBox.Right - childSize.Width;
			localPos.y = parentBox.Top - childSize.Height - Offset;
		}
		else if ( Position == PositionModes.LeftStart )
		{
			localPos.x = parentBox.Left - childSize.Width - Offset;
			localPos.y = parentBox.Top;
		}
		else if ( Position == PositionModes.LeftEnd )
		{
			localPos.x = parentBox.Left - childSize.Width - Offset;
			localPos.y = parentBox.Bottom - childSize.Height;
		}
		else if ( Position == PositionModes.RightStart )
		{
			localPos.x = parentBox.Right + Offset;
			localPos.y = parentBox.Top;
		}
		else if ( Position == PositionModes.RightEnd )
		{
			localPos.x = parentBox.Right + Offset;
			localPos.y = parentBox.Bottom - childSize.Height;
		}

		child.Position = localPos;

		if ( ConstrainToScreen )
		{
			child.ConstrainToScreen();
		}
	}
}
