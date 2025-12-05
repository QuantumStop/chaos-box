using System.Text.Json.Serialization;

namespace Editor.ShaderGraph;

public enum UIType
{
	Default,
	Slider,
	Color,
}

public struct ParameterUI
{
	/// <summary>
	/// Control type used in the material editor
	/// </summary>
	public UIType Type { get; set; }

	/// <summary>
	/// Step amount for sliders
	/// </summary>
	public float Step { get; set; }

	/// <summary>
	/// Priority of this value in the group
	/// </summary>
	public int Priority { get; set; }

	/// <summary>
	/// Primary group
	/// </summary>
	[InlineEditor( Label = false ), Group( "Group" )]
	public UIGroup PrimaryGroup { get; set; }

	/// <summary>
	/// Group within the primary group
	/// </summary>
	[InlineEditor( Label = false ), Group( "Sub Group" )]
	public UIGroup SecondaryGroup { get; set; }

	[JsonIgnore, Hide]
	public readonly string UIGroup => $"{PrimaryGroup.Name},{PrimaryGroup.Priority}/{SecondaryGroup.Name},{SecondaryGroup.Priority}/{Priority}";
}
