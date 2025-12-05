using System;

namespace Editor;

public class ExpandGroup : Widget
{
	Widget widget;

	public string Title { get; set; } = "Untitled Group";
	public string Icon { get; set; }

	protected int headerSize;

	public Action OnCreateWidget { get; set; }

	bool openState;

	public ExpandGroup( Widget parent ) : base( parent )
	{
		SetHeaderSize( (int)(Theme.RowHeight * 1.5f) );
	}

	public void SetWidget( Widget w )
	{
		widget?.Destroy();

		widget = w;
		widget.Parent = this;
		widget.Position = new Vector2( 0, headerSize );
		widget.AdjustSize();
		widget.Width = Width;

		Update();
		DoLayout();
	}

	public void SetHeaderSize( int height )
	{
		headerSize = height;
		MinimumSize = height;

		if ( widget.IsValid() )
			widget.Position = new Vector2( 0, headerSize );
	}

	protected override void DoLayout()
	{
		base.DoLayout();

		if ( widget.IsValid() )
		{
			widget.AdjustSize();
			widget.Width = Width;

			if ( !Animate.IsActive( this ) )
				FixedHeight = IdealHeight;
		}
	}

	protected override void OnPaint()
	{
		bool isExpanded = openState;

		var headerRect = new Rect( 0, 0, Width, headerSize );
		headerRect.Bottom--;

		Paint.ClearPen();
		Paint.SetBrush( Theme.ButtonBackground.WithAlpha( 0.1f ) );
		Paint.DrawRect( LocalRect.Shrink( 0, 1 ), 4.0f );

		Paint.ClearBrush();

		var rect = new Rect( 0, Size );

		rect.Height = headerSize;

		Paint.SetPen( Theme.Text.WithAlpha( isExpanded ? 0.5f : 0.2f ) );
		Paint.DrawIcon( headerRect.Shrink( 4, 0 ), isExpanded ? "arrow_drop_down" : "arrow_right", 24, TextFlag.RightCenter );

		rect.Left += 14;

		if ( !string.IsNullOrWhiteSpace( Icon ) )
		{
			Paint.SetPen( Theme.Text.WithAlpha( isExpanded ? 0.8f : 0.4f ) );
			Paint.DrawIcon( headerRect.Shrink( rect.Left, 0, 0, 0 ), Icon, 18, TextFlag.LeftCenter );

			rect.Left += 34;
		}

		Paint.SetDefaultFont( 8, 400 );
		Paint.SetPen( Theme.Text.WithAlpha( isExpanded ? 1.0f : 0.4f ) );
		Paint.DrawText( headerRect.Shrink( rect.Left, 0, 0, 0 ), Title, TextFlag.LeftCenter );

		if ( !isExpanded )
			return;

		var bodyRect = new Rect( 0, headerSize, Width, Height - headerSize );
		bodyRect.Bottom--;
		bodyRect.Right--;
	}

	public virtual void SetOpenState( bool state )
	{
		if ( openState == state )
			return;

		openState = state;

		if ( !state && !widget.IsValid() )
			return;

		if ( state && !widget.IsValid() )
		{
			if ( OnCreateWidget != null )
			{
				OnCreateWidget?.Invoke();
				BindSystem.Tick();
			}

			OnOpenStateChanged( true );
			return;
		}

		OnOpenStateChanged( state );
	}

	protected override void OnDoubleClick( MouseEvent e )
	{

	}

	protected override void OnMousePress( MouseEvent e )
	{
		base.OnMousePress( e );

		if ( e.LeftMouseButton && e.LocalPosition.y < headerSize )
		{
			var oldHeight = Height;

			SetOpenState( !openState );

			Animate.CancelAll( this, true );

			if ( openState ) Animate.Add( this, 0.5f, oldHeight, IdealHeight, x => { FixedHeight = x; }, "bounce-out" );
			else Animate.Add( this, 0.2f, oldHeight, IdealHeight, x => { FixedHeight = x; }, "ease-out" );
		}
	}

	protected virtual void OnOpenStateChanged( bool newState )
	{
		if ( !string.IsNullOrEmpty( StateCookieName ) )
		{
			ProjectCookie.Set( StateCookieName, newState );
		}

		if ( newState && widget.IsValid() )
			widget.Visible = true;
	}

	float IdealHeight
	{
		get
		{
			float height = headerSize;
			if ( widget.IsValid() && openState )
			{
				height += widget.Height;
			}

			return height;
		}
	}

	public void SetHeight()
	{
		Animate.CancelAll( this, true );
		FixedHeight = IdealHeight;
		Update();
	}

	string _stateCookieName;
	public string StateCookieName
	{
		get => _stateCookieName;
		set
		{
			if ( _stateCookieName == value ) return;
			_stateCookieName = value;

			var state = ProjectCookie.Get( _stateCookieName, openState );
			SetOpenState( state );
			SetHeight();
		}
	}
}
