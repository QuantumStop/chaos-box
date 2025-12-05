
namespace Editor.MeshEditor;

[Title( "Block" ), Icon( "rectangle" )]
public class BlockPrimitive : PrimitiveBuilder
{
	public bool Top { get; set; } = true;
	public bool Bottom { get; set; } = true;
	public bool Left { get; set; } = true;
	public bool Right { get; set; } = true;
	public bool Front { get; set; } = true;
	public bool Back { get; set; } = true;
	public bool Hollow { get; set; } = false;

	[Hide] private BBox _box;

	public override void SetFromBox( BBox box ) => _box = box;

	public override void Build( PolygonMesh mesh )
	{
		Vector3 mins;
		Vector3 maxs;

		if ( Hollow )
		{
			mins = _box.Maxs;
			maxs = _box.Mins;
		}
		else
		{
			mins = _box.Mins;
			maxs = _box.Maxs;
		}

		if ( Top )
		{
			// x planes - top first
			mesh.AddFace(
				new Vector3( mins.x, mins.y, maxs.z ),
				new Vector3( maxs.x, mins.y, maxs.z ),
				new Vector3( maxs.x, maxs.y, maxs.z ),
				new Vector3( mins.x, maxs.y, maxs.z )
			);
		}

		if ( Bottom )
		{
			// x planes - bottom
			mesh.AddFace(
				new Vector3( mins.x, maxs.y, mins.z ),
				new Vector3( maxs.x, maxs.y, mins.z ),
				new Vector3( maxs.x, mins.y, mins.z ),
				new Vector3( mins.x, mins.y, mins.z )
			);
		}

		if ( Left )
		{
			// y planes - left
			mesh.AddFace(
				new Vector3( mins.x, maxs.y, mins.z ),
				new Vector3( mins.x, mins.y, mins.z ),
				new Vector3( mins.x, mins.y, maxs.z ),
				new Vector3( mins.x, maxs.y, maxs.z )
			);
		}

		if ( Right )
		{
			// y planes - right
			mesh.AddFace(
				new Vector3( maxs.x, maxs.y, maxs.z ),
				new Vector3( maxs.x, mins.y, maxs.z ),
				new Vector3( maxs.x, mins.y, mins.z ),
				new Vector3( maxs.x, maxs.y, mins.z )
			);
		}

		if ( Front )
		{
			// x planes - farthest
			mesh.AddFace(
				new Vector3( maxs.x, maxs.y, mins.z ),
				new Vector3( mins.x, maxs.y, mins.z ),
				new Vector3( mins.x, maxs.y, maxs.z ),
				new Vector3( maxs.x, maxs.y, maxs.z )
			);
		}

		if ( Back )
		{
			// x planes - nearest
			mesh.AddFace(
				new Vector3( maxs.x, mins.y, maxs.z ),
				new Vector3( mins.x, mins.y, maxs.z ),
				new Vector3( mins.x, mins.y, mins.z ),
				new Vector3( maxs.x, mins.y, mins.z )
			);
		}
	}
}
