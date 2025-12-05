using System;

namespace Editor;

[AttributeUsage( AttributeTargets.Class )]
public class DropObjectAttribute : System.Attribute
{
	public string Type { get; }
	public string[] Extensions { get; }
	public DropObjectAttribute( string type, params string[] extensions )
	{
		Type = type;
		Extensions = extensions;
	}
}
