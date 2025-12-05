namespace Editor;

[AttributeUsage( AttributeTargets.Class )]
public class AssetPickerAttribute : Attribute
{
	public Type[] ResourceTypes { get; init; }

	public AssetPickerAttribute( params Type[] type )
	{
		ResourceTypes = type;
	}
}
