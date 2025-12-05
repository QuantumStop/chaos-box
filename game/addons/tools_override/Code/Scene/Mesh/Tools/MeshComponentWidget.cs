using Sandbox.UI;
using System.IO;

namespace Editor.MeshEditor;

[CustomEditor( typeof( MeshComponent ) )]
public class MeshComponentWidget : ComponentEditorWidget
{
	private ControlSheet ControlSheet;

	private static bool FilterProperties( SerializedProperty o )
	{
		if ( !o.HasAttribute<PropertyAttribute>() ) return false;
		if ( o.Name == nameof( MeshComponent.Mesh ) ) return false;
		if ( o.PropertyType.IsAssignableTo( typeof( Delegate ) ) && o.Name.StartsWith( "OnComponent" ) ) return false;

		return true;
	}

	public MeshComponentWidget( SerializedObject obj ) : base( obj )
	{
		Layout = Layout.Column();
		Layout.Margin = 0;

		RebuildUI();
	}

	void RebuildUI()
	{
		Layout.Clear( true );

		var layout = Layout.AddColumn();
		layout.Margin = 8;

		var row = layout.AddRow();
		row.Margin = 8;
		row.Spacing = 8;
		row.AddStretchCell();
		row.Add( new Button.Primary( "Import..." ) { Enabled = false } );
		row.Add( new Button.Primary( "Export..." ) { Clicked = ConvertToModel } );

		ControlSheet = new ControlSheet();
		ControlSheet.Margin = 0;
		ControlSheet.AddObject( SerializedObject, FilterProperties );

		var so = this.GetSerialized();
		ControlSheet.AddGroup( "Operations", new[]
		{
			so.GetProperty( nameof( CenterOrigin ) ),
		} );
		Layout.Add( ControlSheet );
	}

	private void ConvertToModel()
	{
		var targetPath = EditorUtility.SaveFileDialog( "Create Model..", "vmdl", "" );
		if ( targetPath is null )
			return;

		var meshes = SerializedObject.Targets.OfType<MeshComponent>()
			.Select( x => x.Mesh )
			.ToArray();

		EditorUtility.CreateModelFromPolygonMeshes( meshes, targetPath );
	}

	[Button]
	public void CenterOrigin()
	{
		foreach ( var target in SerializedObject.Targets.OfType<MeshComponent>() )
			CenterMeshOrigin( target );
	}

	private static void CenterMeshOrigin( MeshComponent meshComponent )
	{
		if ( !meshComponent.IsValid() )
			return;

		var mesh = meshComponent.Mesh;
		if ( mesh is null )
			return;

		var children = meshComponent.GameObject.Children
			.Select( x => (GameObject: x, Transform: x.WorldTransform) )
			.ToArray();

		var world = meshComponent.WorldTransform;
		var bounds = mesh.CalculateBounds( world );
		var center = bounds.Center;
		var localCenter = world.PointToLocal( center );
		meshComponent.WorldPosition = center;
		meshComponent.Mesh.ApplyTransform( new Transform( -localCenter ) );
		meshComponent.RebuildMesh();

		foreach ( var child in children )
			child.GameObject.WorldTransform = child.Transform;
	}
}
