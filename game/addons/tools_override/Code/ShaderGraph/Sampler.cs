
namespace Editor.ShaderGraph;

public enum SamplerFilter
{
	Aniso,
	Bilinear,
	Trilinear,
	Point,
}

public enum SamplerAddress
{
	Wrap,
	Mirror,
	Clamp,
	Border,
	Mirror_Once,
}

public struct Sampler
{
	/// <summary>
	/// Smooth or Pixelated filtering
	/// </summary>
	public SamplerFilter Filter { get; set; }

	/// <summary>
	/// Horizontal wrapping, repeating or stretched
	/// </summary>
	public SamplerAddress AddressU { get; set; }

	/// <summary>
	/// Vertical wrapping, repeating or stretched
	/// </summary>
	public SamplerAddress AddressV { get; set; }
}
