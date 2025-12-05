using System.IO;
using System.Text.RegularExpressions;

namespace Editor;

public static class PathExtensions
{
	/// <summary>
	/// Returns a full path in the same directory as <paramref name="fileInfo"/>, but with the given <paramref name="newName"/>.
	/// </summary>
	public static string GetNewPath( this FileInfo fileInfo, string newName )
	{
		return Path.Combine( fileInfo.DirectoryName ?? "", newName );
	}

	private static Regex DuplicateNameRegex { get; } = new( "(?<name>.+) - (?<index>[0-9]+)$" );

	private static bool TryParseDuplicateName( string name, out string baseName, out int index )
	{
		if ( DuplicateNameRegex.Match( name ) is { Success: true } match )
		{
			baseName = match.Groups["name"].Value;
			index = int.Parse( match.Groups["index"].Value );
			return true;
		}

		baseName = null;
		index = default;
		return false;
	}

	/// <summary>
	/// Generates a new name for <paramref name="fileInfo"/> appended with a number like <c>" - 123"</c>.
	/// Will choose a number that doesn't already exist, starting at <c>2</c>. If the original name already
	/// ends with a number, will find the next highest number that doesn't exist.
	/// </summary>
	public static string GetDefaultDuplicateName( this FileInfo fileInfo )
	{
		var name = Path.GetFileNameWithoutExtension( fileInfo.Name );
		var ext = fileInfo.Extension;

		if ( TryParseDuplicateName( name, out var baseName, out var index ) )
		{
			name = baseName;
			index += 1;
		}
		else
		{
			index = 2;
		}

		string newName;

		while ( File.Exists( fileInfo.GetNewPath( newName = $"{name} - {index}{ext}" ) ) )
		{
			++index;
		}

		return newName;
	}
}
