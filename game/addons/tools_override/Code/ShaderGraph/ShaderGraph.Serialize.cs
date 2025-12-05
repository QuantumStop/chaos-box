using Editor.NodeEditor;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Editor.ShaderGraph;

partial class ShaderGraph
{
	private static JsonSerializerOptions SerializerOptions( bool indented = false )
	{
		var options = new JsonSerializerOptions
		{
			WriteIndented = indented,
			PropertyNameCaseInsensitive = true,
			NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
			DefaultIgnoreCondition = JsonIgnoreCondition.Never,
			ReadCommentHandling = JsonCommentHandling.Skip,
		};

		options.Converters.Add( new JsonStringEnumConverter( null, true ) );

		return options;
	}

	public string Serialize()
	{
		var doc = new JsonObject();
		var options = SerializerOptions( true );

		SerializeObject( this, doc, options );
		SerializeNodes( Nodes, doc, options );

		return doc.ToJsonString( options );
	}

	public void Deserialize( string json, string subgraphPath = null )
	{
		using var doc = JsonDocument.Parse( json );
		var root = doc.RootElement;
		var options = SerializerOptions();

		// Check for the version so we can handle upgrades
		var latestVersion = Version;
		var currentVersion = 0; // Assume 0 for files that don't have the Version property
		if ( root.TryGetProperty( "Version", out var ver ) )
		{
			currentVersion = ver.GetInt32();
		}

		// Deserialize everything using the current version
		Version = currentVersion;
		DeserializeObject( this, root, options );
		DeserializeNodes( root, options, subgraphPath, currentVersion );

		// Upgrade to the latest version
		Version = latestVersion;
	}

	public IEnumerable<BaseNode> DeserializeNodes( string json )
	{
		using var doc = JsonDocument.Parse( json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip } );
		var root = doc.RootElement;

		// Check for version in the JSON
		var fileVersion = 1; // Default to current version
		if ( root.TryGetProperty( "Version", out var ver ) )
		{
			fileVersion = ver.GetInt32();
		}
		else
		{
			fileVersion = 0; // Old file without version
		}

		return DeserializeNodes( root, SerializerOptions(), null, fileVersion );
	}

	private static void DeserializeObject( object obj, JsonElement doc, JsonSerializerOptions options )
	{
		var type = obj.GetType();
		var properties = type.GetProperties( BindingFlags.Instance | BindingFlags.Public )
			.Where( x => x.GetSetMethod() != null );

		foreach ( var nodeProperty in doc.EnumerateObject() )
		{
			var prop = properties.FirstOrDefault( x =>
			{
				var propName = x.Name;
				if ( x.GetCustomAttribute<JsonPropertyNameAttribute>() is JsonPropertyNameAttribute jpna )
					propName = jpna.Name;

				return string.Equals( propName, nodeProperty.Name, StringComparison.OrdinalIgnoreCase );
			} );

			if ( prop == null )
				continue;

			if ( prop.CanWrite == false )
				continue;

			if ( prop.IsDefined( typeof( JsonIgnoreAttribute ) ) )
				continue;

			prop.SetValue( obj, JsonSerializer.Deserialize( nodeProperty.Value.GetRawText(), prop.PropertyType, options ) );
		}
	}

	private IEnumerable<BaseNode> DeserializeNodes( JsonElement doc, JsonSerializerOptions options, string subgraphPath = null, int fileVersion = -1 )
	{
		var nodes = new Dictionary<string, BaseNode>();
		var identifiers = _nodes.Count > 0 ? new Dictionary<string, string>() : null;
		var connections = new List<(IPlugIn Plug, NodeInput Value)>();

		var arrayProperty = doc.GetProperty( "nodes" );
		foreach ( var element in arrayProperty.EnumerateArray() )
		{
			var typeName = element.GetProperty( "_class" ).GetString();
			var typeDesc = EditorTypeLibrary.GetType( typeName );
			var type = new ClassNodeType( typeDesc );

			BaseNode node;
			if ( typeDesc is null )
			{
				var missingNode = new MissingNode( typeName, element );
				node = missingNode;
				DeserializeObject( node, element, options );
			}
			else
			{
				// Check if this is a legacy parameter node that should be upgraded to SubgraphInput
				// Only upgrade for old subgraph files (files without Version property aka. 0 -> 1)
				if ( IsSubgraph && fileVersion < 1 && ShouldUpgradeToSubgraphInput( typeName, element ) )
				{
					node = CreateUpgradedSubgraphInput( typeName, element, options );
				}
				else
				{
					node = EditorTypeLibrary.Create<BaseNode>( typeName );
					DeserializeObject( node, element, options );
				}

				if ( identifiers != null && _nodes.ContainsKey( node.Identifier ) )
				{
					identifiers.Add( node.Identifier, node.NewIdentifier() );
				}

				if ( node is FunctionResult funcResult )
				{
					funcResult.CreateInputs();
				}

				if ( node is SubgraphNode subgraphNode )
				{
					if ( !FileSystem.Content.FileExists( subgraphNode.SubgraphPath ) )
					{
						var missingNode = new MissingNode( typeName, element );
						node = missingNode;
						DeserializeObject( node, element, options );
					}
					else
					{
						subgraphNode.OnNodeCreated();
					}
				}

				foreach ( var input in node.Inputs )
				{
					if ( !element.TryGetProperty( input.Identifier, out var connectedElem ) )
						continue;

					var connected = connectedElem
						.Deserialize<NodeInput?>();

					if ( connected is { IsValid: true } )
					{
						var connection = connected.Value;
						if ( !string.IsNullOrEmpty( subgraphPath ) )
						{
							connection = new()
							{
								Identifier = connection.Identifier,
								Output = connection.Output,
								Subgraph = subgraphPath
							};
						}
						connections.Add( (input, connection) );
					}
				}
			}

			nodes.Add( node.Identifier, node );

			AddNode( node );
		}

		foreach ( var (input, value) in connections )
		{
			var outputIdent = identifiers?.TryGetValue( value.Identifier, out var newIdent ) ?? false
				? newIdent : value.Identifier;

			if ( nodes.TryGetValue( outputIdent, out var node ) )
			{
				var output = node.Outputs.FirstOrDefault( x => x.Identifier == value.Output );
				if ( output is null )
				{
					// Check for Aliases
					foreach ( var op in node.Outputs )
					{
						if ( op is not BasePlugOut plugOut ) continue;

						var aliasAttr = plugOut.Info.Property?.GetCustomAttribute<AliasAttribute>();
						if ( aliasAttr is not null && aliasAttr.Value.Contains( value.Output ) )
						{
							output = plugOut;
							break;
						}
					}
				}
				input.ConnectedOutput = output;
			}
		}

		return nodes.Values;
	}

	public string SerializeNodes()
	{
		return SerializeNodes( Nodes );
	}

	public string SerializeNodes( IEnumerable<BaseNode> nodes )
	{
		var doc = new JsonObject();
		var options = SerializerOptions();

		SerializeNodes( nodes, doc, options );

		return doc.ToJsonString( options );
	}

	private static void SerializeObject( object obj, JsonObject doc, JsonSerializerOptions options, Dictionary<string, string> identifiers = null )
	{
		var type = obj.GetType();
		var properties = type.GetProperties( BindingFlags.Instance | BindingFlags.Public )
			.Where( x => x.GetSetMethod() != null );

		foreach ( var property in properties )
		{
			if ( !property.CanRead )
				continue;

			if ( property.PropertyType == typeof( NodeInput ) )
				continue;

			if ( property.IsDefined( typeof( JsonIgnoreAttribute ) ) )
				continue;

			var propertyName = property.Name;
			if ( property.GetCustomAttribute<JsonPropertyNameAttribute>() is { } jpna )
				propertyName = jpna.Name;

			var propertyValue = property.GetValue( obj );
			if ( propertyName == "Identifier" && propertyValue is string identifier )
			{
				if ( identifiers.TryGetValue( identifier, out var newIdentifier ) )
				{
					propertyValue = newIdentifier;
				}
			}

			doc.Add( propertyName, JsonSerializer.SerializeToNode( propertyValue, options ) );
		}

		if ( obj is INode node )
		{
			foreach ( var input in node.Inputs )
			{
				if ( input.ConnectedOutput is not { } output )
					continue;

				doc.Add( input.Identifier, JsonSerializer.SerializeToNode( new NodeInput
				{
					Identifier = identifiers?.TryGetValue( output.Node.Identifier, out var newIdent ) ?? false ? newIdent : output.Node.Identifier,
					Output = output.Identifier
				} ) );
			}
		}
	}

	private static void SerializeNodes( IEnumerable<BaseNode> nodes, JsonObject doc, JsonSerializerOptions options )
	{
		var identifiers = new Dictionary<string, string>();
		foreach ( var node in nodes )
		{
			identifiers.Add( node.Identifier, $"{identifiers.Count}" );
		}

		var nodeArray = new JsonArray();

		foreach ( var node in nodes )
		{
			var type = node.GetType();
			var nodeObject = new JsonObject { { "_class", type.Name } };

			SerializeObject( node, nodeObject, options, identifiers );

			nodeArray.Add( nodeObject );
		}

		doc.Add( "nodes", nodeArray );
	}

	/// <summary>
	/// Check if a legacy parameter node should be upgraded to SubgraphInput.
	/// </summary>
	private static bool ShouldUpgradeToSubgraphInput( string typeName, JsonElement element )
	{
		// Only upgrade if it's a parameter node type
		if ( !IsParameterNodeType( typeName ) )
			return false;

		// Only upgrade if it has a name (indicating it's meant to be an input)
		if ( element.TryGetProperty( "Name", out var nameProperty ) )
		{
			var name = nameProperty.GetString();
			return !string.IsNullOrWhiteSpace( name );
		}

		return false;
	}

	/// <summary>
	/// Check if the type name represents a parameter node
	/// </summary>
	private static bool IsParameterNodeType( string typeName )
	{
		return typeName switch
		{
			"Float" => true,
			"Float2" => true,
			"Float3" => true,
			"Float4" => true,
			"TextureSampler" => true,
			_ => false
		};
	}

	/// <summary>
	/// Create a new SubgraphInput node from a legacy parameter node
	/// </summary>
	private SubgraphInput CreateUpgradedSubgraphInput( string typeName, JsonElement element, JsonSerializerOptions options )
	{
		var subgraphInput = new SubgraphInput();

		// Copy basic node properties
		DeserializeObject( subgraphInput, element, options );

		// Set input name from the parameter's Name property
		if ( element.TryGetProperty( "Name", out var nameProperty ) )
		{
			subgraphInput.InputName = nameProperty.GetString();
		}

		// Map the parameter type to InputType and set default values
		switch ( typeName )
		{
			case "Float":
				subgraphInput.InputType = InputType.Float;
				if ( element.TryGetProperty( "Value", out var floatValue ) )
				{
					subgraphInput.DefaultFloat = floatValue.GetSingle();
				}
				break;

			case "Float2":
				subgraphInput.InputType = InputType.Float2;
				if ( element.TryGetProperty( "Value", out var float2Value ) )
				{
					var vector2 = JsonSerializer.Deserialize<Vector2>( float2Value.GetRawText(), options );
					subgraphInput.DefaultFloat2 = vector2;
				}
				break;

			case "Float3":
				subgraphInput.InputType = InputType.Float3;
				if ( element.TryGetProperty( "Value", out var float3Value ) )
				{
					var vector3 = JsonSerializer.Deserialize<Vector3>( float3Value.GetRawText(), options );
					subgraphInput.DefaultFloat3 = vector3;
				}
				break;

			case "Float4":
				subgraphInput.InputType = InputType.Color;
				if ( element.TryGetProperty( "Value", out var float4Value ) )
				{
					var color = JsonSerializer.Deserialize<Color>( float4Value.GetRawText(), options );
					subgraphInput.DefaultColor = color;
				}
				break;
		}

		return subgraphInput;
	}
}
