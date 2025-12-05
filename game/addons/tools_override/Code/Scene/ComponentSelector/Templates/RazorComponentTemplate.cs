namespace Editor;

[Icon( "dashboard" )]
[Title( "Razor Panel Component" )]
[Description( "A razor panel component demonstrating how to make a UI panel through a component." )]
public partial class RazorComponentTemplate : ComponentTemplate
{
	// Needed for the file dialog
	public override string NameFilter => "Razor File (*.razor)";
	public override string Suffix => ".razor";

	public override void Create( string componentName, string path )
	{
		var ns = "Sandbox";

		if ( Project.Current.Config.TryGetMeta<Compiler.Configuration>( "Compiler", out var compilerSettings ) )
		{
			ns = compilerSettings.RootNamespace;
		}

		var razor = $$"""
		@using Sandbox;
		@using Sandbox.UI;
		@inherits PanelComponent
		@namespace {{ns}}

		<root>
			<div class="title">@MyStringValue</div>
		</root>

		@code
		{

			[Property, TextArea] public string MyStringValue { get; set; } = "Hello World!";

			/// <summary>
			/// the hash determines if the system should be rebuilt. If it changes, it will be rebuilt
			/// </summary>
			protected override int BuildHash() => System.HashCode.Combine( MyStringValue );
		}
		""";

		var directory = System.IO.Path.GetDirectoryName( path );
		System.IO.File.WriteAllText( System.IO.Path.Combine( directory, componentName + Suffix ), razor );

		var scss = $$"""
		{{componentName}}
		{
			position: absolute;
			top: 0;
			left: 0;
			right: 0;
			bottom: 0;
			background-color: #444;
			justify-content: center;
			align-items: center;
			font-weight: bold;
			border-radius: 20px;

			.title
			{
				font-size: 25px;
				font-family: Poppins;
				color: #fff;
			}
		}
		""";

		System.IO.File.WriteAllText( System.IO.Path.Combine( directory, componentName + ".razor.scss" ), scss );
	}
}
