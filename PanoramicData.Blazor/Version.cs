namespace PanoramicData.Blazor;

/// <summary>
/// Exposes the assembly file version for use at runtime (e.g. in cache-busting URLs).
/// </summary>
public static class Version
{
	/// <summary>
	/// Gets the file version string embedded in the assembly at build time.
	/// </summary>
	public static string AssemblyFileVersion
	{
		get
		{
			return ThisAssembly.AssemblyFileVersion;
		}
	}
}
