using Sandbox.Audio;

namespace Editor;

/// <summary>
/// Dropdown selection for DspPresetHandle
/// </summary>
[CustomEditor( typeof( DspPresetHandle ) )]
public sealed class DspPresetHandleControlWidget : DropdownControlWidget<DspPresetHandle>
{
	public DspPresetHandleControlWidget( SerializedProperty property ) : base( property )
	{
	}

	protected override IEnumerable<object> GetDropdownValues()
	{
		return Sound.DspNames.Select( x => (DspPresetHandle)x ).Cast<object>();
	}
}
