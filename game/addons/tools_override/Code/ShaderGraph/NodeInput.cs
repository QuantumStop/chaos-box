using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Editor.ShaderGraph;

public struct NodeInput : IValid
{
	[Hide, Browsable( false )]
	public string Identifier { get; set; }

	[Hide, Browsable( false )]
	public string Output { get; set; }

	[Hide, Browsable( false )]
	[JsonIgnore]
	public string Subgraph { get; set; }

	[Hide, Browsable( false )]
	[JsonIgnore]
	public string SubgraphNode { get; set; }

	[Browsable( false )]
	[JsonIgnore, Hide]
	public readonly bool IsValid => !string.IsNullOrWhiteSpace( Identifier ) && !string.IsNullOrWhiteSpace( Output );

	public override readonly string ToString()
	{
		var subgraph = (Subgraph is not null) ? ("." + Subgraph) : "";
		var subgraphNode = (SubgraphNode is not null) ? ("." + SubgraphNode) : "";
		return IsValid ? $"{Identifier}.{Output}{subgraph}{subgraphNode}" : "null";
	}

	public NodeInput()
	{
		Identifier = "";
		Output = "";
		Subgraph = null;
	}

	public static bool operator ==( NodeInput a, NodeInput b ) => a.Identifier == b.Identifier && a.Output == b.Output && a.Subgraph == b.Subgraph && a.SubgraphNode == b.SubgraphNode;
	public static bool operator !=( NodeInput a, NodeInput b ) => a.Identifier != b.Identifier || a.Output != b.Output || a.Subgraph != b.Subgraph || a.SubgraphNode != b.SubgraphNode;
	public override bool Equals( object obj ) => obj is NodeInput input && this == input;
	public override int GetHashCode() => System.HashCode.Combine( Identifier, Output, Subgraph, SubgraphNode );
}
