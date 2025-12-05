using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Editor.ShaderGraph;

public class MissingNode : BaseNode
{
	[Hide]
	public string Title { get; set; }

	[Hide]
	private string _content = "";

	[Hide]
	public string Content
	{
		get => _content;
		set
		{
			_content = value;
			Paint.SetDefaultFont();
			ContentSize = Paint.MeasureText( Content );
			ExpandSize = new Vector2( 30, ContentSize.y + 16 );
		}
	}

	[Hide]
	Vector2 ContentSize = new();

	[Hide]
	public override Color PrimaryColor => Theme.MultipleValues;

	public MissingNode()
	{
	}

	public MissingNode( string title, JsonElement json ) : base()
	{
		Title = title;
		Content = json.ToString();
	}

	public override void OnPaint( Rect rect )
	{
		Paint.SetDefaultFont();
		Paint.DrawText( rect.Shrink( 8, 22, 8, 8 ), Content, TextFlag.LeftTop );
	}

	[JsonIgnore, Hide, Browsable( false )]
	public override DisplayInfo DisplayInfo
	{
		get
		{
			var info = base.DisplayInfo;
			info.Name = "Missing " + Title ?? info.Name;
			info.Icon = "error";
			return info;
		}
	}
}
