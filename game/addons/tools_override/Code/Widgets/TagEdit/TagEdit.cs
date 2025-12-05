using Sandbox.UI;

namespace Editor;

[CustomEditor( typeof( string ), NamedEditor = "tags" )]
public class TagsControlWidget : ControlWidget
{
	private readonly TagEdit widget;

	public TagsControlWidget( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Row();
		widget = Layout.Add( new TagEdit( this ) );
		widget.Value = property.GetValue<string>();
		widget.OnEdited = () => SerializedProperty.As.String = widget.Value;
	}

	protected override void OnValueChanged()
	{
		base.OnValueChanged();

		widget.Value = SerializedProperty.As.String;
	}
}

/// <summary>
/// A text entry that automatically breaks the input into tags
/// </summary>
[CanEdit( typeof( string ), "tags" )]
public partial class TagEdit : Widget
{
	/// <summary>
	/// Returned by <see cref="Convertors"/> on successfully parsing a tag
	/// </summary>
	public class TagDetail
	{
		public string Title { get; set; }
		public string Value { get; set; }
		public string Icon { get; set; } = "local_offer";
		public Color Color { get; set; } = Theme.Green;
		public bool Hidden { get; set; }
	}

	/// <summary>
	/// Called when the contents are edited, either by typing or removing a tag
	/// </summary>
	public Action OnEdited { get; set; }

	public List<Func<string, TagDetail>> Convertors { get; init; } = new();

	Func<string, TagDetail> DefaultConvertor => tag =>
	{
		tag = tag?.ToLowerInvariant();

		if ( !tag.IsValidTag() )
			return null;

		return new TagEdit.TagDetail
		{
			Value = tag,
			Title = tag,
		};
	};

	public LineEdit LineEdit { get; set; }

	Layout TagsLayout { get; set; }

	List<TagEntry> Tags = new();

	string _value;
	string _valueTags;

	/// <summary>
	/// Outputs the tags followed by any unswallowed text
	/// </summary>
	public string Value
	{
		get => _value;
		set
		{
			if ( _value == value )
				return;

			TagsLayout.Clear( true );
			Tags.Clear();

			_value = value;
			LineEdit.Text = _value;
			TryConvertTags();
		}
	}

	/// <summary>
	/// Like Value but only outputs accepted tags
	/// </summary>
	public string ValueTags
	{
		get => _valueTags;
		set => Value = value;
	}

	public TagEdit( Widget parent = null ) : base( parent )
	{
		FixedHeight = Theme.RowHeight;
		MinimumWidth = Theme.RowHeight;

		LineEdit = new LineEdit( this );
		LineEdit.FixedHeight = Theme.RowHeight;
		LineEdit.TextEdited += LineEdit_TextEdited;
		LineEdit.EditingFinished += LineEdit_EditingFinished;

		Layout = Layout.Row();

		TagsLayout = Layout.AddRow();
		TagsLayout.Spacing = 2;

		Layout.Add( LineEdit, 1 );
		Layout.Margin = 0;
	}

	private void LineEdit_EditingFinished()
	{
		TryConvertTags();
		FireOnEditedIfValueChanged();
	}

	private string OldValue;
	private void FireOnEditedIfValueChanged()
	{
		if ( OldValue == Value ) return;
		OldValue = Value;

		OnEdited?.Invoke();
	}

	private void LineEdit_TextEdited( string value )
	{
		if ( !char.IsWhiteSpace( value.LastOrDefault() ) ) return;

		TryConvertTags();
		FireOnEditedIfValueChanged();
	}

	public void TryConvertTags()
	{
		string value = LineEdit.Text;
		if ( value.Length < 1 )
		{
			RebuildValue();
			return;
		}

		bool focused = LineEdit.IsFocused;

		var parts = value.SplitQuotesStrings();
		if ( parts.Length <= 0 )
		{
			RebuildValue();
			return;
		}

		var partList = parts.ToList();
		var arrayParts = parts.ToArray();

		foreach ( var part in arrayParts )
		{
			if ( focused && arrayParts.Last() == part && !value.EndsWith( " " ) )
				continue;

			if ( TryConvertTag( part ) )
			{
				partList.Remove( part );
			}
		}

		var text = string.Join( ' ', partList.Select( x => x.QuoteSafe( true ) ) );

		if ( value.EndsWith( " " ) )
			text += " ";

		LineEdit.Text = text;
		RebuildValue();
	}

	bool TryConvertTag( string tag )
	{
		if ( !Convertors.Any() )
		{
			var r = DefaultConvertor?.Invoke( tag );
			if ( r == null ) return false;

			// Prevent duplicate tags
			if ( Tags.Any( x => x.Detail.Value == r.Value ) ) return true;

			var widget = new TagEntry( this, r );
			Tags.Add( widget );
			TagsLayout.Add( widget );
			return true;
		}

		foreach ( var t in Convertors )
		{
			var r = t?.Invoke( tag );
			if ( r == null ) continue;

			// Prevent duplicate tags
			if ( Tags.Any( x => x.Detail.Value == r.Value ) ) return true;

			var widget = new TagEntry( this, r );
			Tags.Add( widget );
			TagsLayout.Add( widget );
			return true;
		}

		return false;
	}

	void OnTagClicked( TagEntry tag )
	{
		tag?.Destroy();

		if ( Tags.Remove( tag ) )
		{
			RebuildValue();
			FireOnEditedIfValueChanged();
		}
	}

	void RebuildValue()
	{
		_value = string.Join( " ", Tags.Select( x => x.Detail.Value.QuoteSafe( true ) ) );
		_valueTags = _value;

		var t = LineEdit.Text.Trim();
		if ( !string.IsNullOrWhiteSpace( t ) )
		{
			_value = $"{_value} {t}".Trim();
		}

		TagsLayout.Margin = Tags.Any() ? new Margin( 0, 0, 2, 0 ) : 0;
	}

	internal class TagEntry : Widget
	{
		public TagDetail Detail { get; init; }

		public TagEntry( TagEdit parent, TagDetail tag ) : base( parent )
		{
			Detail = tag;

			FixedHeight = Theme.RowHeight;
			MinimumWidth = Theme.RowHeight;

			ToolTip = $"Click to remove '{tag.Title}'";

			Cursor = CursorShape.Finger;

			if ( tag.Hidden )
			{
				Visible = false;
			}
		}

		protected override Vector2 SizeHint()
		{
			Paint.SetDefaultFont( 7 );
			var size = Paint.MeasureText( Detail.Title );
			if ( size.x > 200 ) size.x = 200;

			return new( size.x + 24 + 24, 32 );
		}

		protected override void OnPaint()
		{
			var col = Detail.Color.Darken( 0.5f );
			var radius = 3;
			var r = LocalRect;

			if ( Paint.HasMouseOver ) col = col.Lighten( 0.1f );
			if ( Paint.HasPressed ) col = col.Lighten( 0.2f );

			Paint.Antialiasing = true;
			Paint.ClearPen();

			Paint.SetBrush( col.Lighten( 0.2f ) );
			Paint.DrawRect( r.Shrink( 0, 0, 1, 1 ), radius );

			Paint.SetBrush( col.Darken( 0.4f ) );
			Paint.DrawRect( r.Shrink( 1, 1, 0, 0 ), radius );

			Paint.SetBrush( col );
			Paint.DrawRect( r.Shrink( 1 ), radius - 1 );

			Paint.SetDefaultFont( 7 );
			Paint.SetPen( Detail.Color.Lighten( 0.2f ) );
			Paint.DrawText( r.Shrink( 24, 0 ), Detail.Title, TextFlag.RightCenter );

			Paint.SetPen( Detail.Color.Lighten( 0.2f ) );
			Paint.DrawIcon( LocalRect.Shrink( 4 ), Detail.Icon, 12, TextFlag.LeftCenter );

			Paint.SetPen( col.Darken( 0.4f ) );
			Paint.DrawIcon( LocalRect.Shrink( 4 ), "clear", 12, TextFlag.RightCenter );

			//base.OnPaint();
		}

		protected override void OnMouseClick( MouseEvent e )
		{
			base.OnMouseClick( e );

			if ( Parent is TagEdit te )
			{
				te.OnTagClicked( this );
			}
		}
	}

	[WidgetGallery]
	[Title( "TagEdit" )]
	[Icon( "account_tree" )]
	internal static Widget WidgetGallery()
	{
		var canvas = new Widget( null );
		canvas.Layout = Layout.Row();
		canvas.Layout.Spacing = 32;

		var view = new TagEdit( canvas );
		view.HorizontalSizeMode = SizeMode.CanGrow;
		view.LineEdit.Text = "garry #nerd @everyone models #newest";

		// add convertor
		view.Convertors.Add( ( tag ) =>
		{
			if ( !tag.StartsWith( "#" ) )
				return null;

			return new TagDetail
			{
				Title = tag.TrimStart( '#' ).ToUpper(),
				Value = tag
			};
		} );

		view.Convertors.Add( ( tag ) =>
		{
			if ( !tag.StartsWith( "@" ) )
				return null;

			return new TagDetail
			{
				Title = tag,
				Value = tag,
				Color = Theme.Blue,
				Icon = "people"
			};
		} );

		view.TryConvertTags();

		var sheet = new ControlSheet();
		sheet.AddProperty( view, x => x.Value );
		sheet.AddProperty( view, x => x.ValueTags );

		var leftCol = canvas.Layout.AddColumn();

		leftCol.Add( view );
		leftCol.AddStretchCell();

		var rightCol = canvas.Layout.AddColumn();
		rightCol.Add( new Label.Subtitle( "Config" ) );
		rightCol.Add( sheet );

		rightCol.AddStretchCell();

		return canvas;
	}
}
