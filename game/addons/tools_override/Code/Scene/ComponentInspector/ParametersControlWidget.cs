using Editor.Inspectors;

namespace Editor;

[CustomEditor( typeof( SkinnedModelRenderer.ParameterAccessor ) )]
public class ParametersControlWidget : ControlWidget
{
	public override bool IncludeLabel => false;

	public ParametersControlWidget( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Column();

		Rebuild();
	}

	protected override void OnPaint()
	{

	}

	public void Rebuild()
	{
		Layout.Clear( true );

		var accessor = SerializedProperty.GetValue<SkinnedModelRenderer.ParameterAccessor>();
		var parameterList = new AnimationParameterList( this );
		parameterList.SetGraph( accessor.Graph );
		parameterList.SetAccessor( new AnimationParameterCollection( SerializedProperty, accessor ) );

		Layout.Add( parameterList );
	}

	protected override void OnValueChanged()
	{
		Rebuild();
	}
}

file class AnimationParameterCollection : AnimationParameterList.IAccessor
{
	private readonly SkinnedModelRenderer.ParameterAccessor accessor;

	public SerializedProperty ParentProperty { get; }

	public AnimationParameterCollection( SerializedProperty parentProperty, SkinnedModelRenderer.ParameterAccessor accessor )
	{
		ParentProperty = parentProperty;

		this.accessor = accessor;
	}

	public bool GetBool( string name ) => accessor.GetBool( name );
	public float GetFloat( string name ) => accessor.GetFloat( name );
	public int GetInt( string name ) => accessor.GetInt( name );
	public Rotation GetRotation( string name ) => accessor.GetRotation( name );
	public Vector3 GetVector( string name ) => accessor.GetVector( name );

	public void Set( string name, bool value ) => accessor.Set( name, value );
	public void Set( string name, float value ) => accessor.Set( name, value );
	public void Set( string name, Vector3 value ) => accessor.Set( name, value );
	public void Set( string name, int value ) => accessor.Set( name, value );
	public void Set( string name, Rotation value ) => accessor.Set( name, value );

	public void Clear() => accessor.Clear();
	public void Clear( string name ) => accessor.Clear( name );
	public bool Contains( string name ) => accessor.Contains( name );
}
