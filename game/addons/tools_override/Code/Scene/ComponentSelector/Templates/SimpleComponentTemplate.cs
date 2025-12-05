using System.Text;

namespace Editor;

[Icon( "check_box_outline_blank" )]
[Title( "Simple Component" )]
[Description( "A simple component with an Update method." )]
public partial class SimpleComponentTemplate : ComponentTemplate
{
	public override void Create( string componentName, string path )
	{
		string ns = null;

		if ( Project.Current.Config.TryGetMeta<Compiler.Configuration>( "Compiler", out var compilerSettings ) )
		{
			ns = compilerSettings.RootNamespace;
		}

		var builder = new StringBuilder();

		if ( ns?.Split( '.' ).FirstOrDefault() != "Sandbox" )
		{
			builder.AppendLine( "using Sandbox;" );
			builder.AppendLine();
		}

		if ( !string.IsNullOrEmpty( ns ) )
		{
			builder.AppendLine( $"namespace {ns};" );
			builder.AppendLine();
		}

		builder.AppendLine( $$"""
			public sealed class {{componentName}} : Component
			{
				protected override void OnUpdate()
				{

				}
			}
			""" );

		var directory = System.IO.Path.GetDirectoryName( path );
		System.IO.File.WriteAllText( System.IO.Path.Combine( directory, componentName + Suffix ), builder.ToString() );
	}
}
