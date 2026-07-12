namespace PanoramicData.Blazor;

/// <summary>
/// Provides versioned JavaScript interop URLs to prevent browsers from caching stale script bundles after package updates.
/// </summary>
public static class JSInteropVersionHelper
{
    /// <summary>
    /// Gets the cache-busted URL for the shared common JavaScript module.
    /// </summary>
    public static string CommonJsUrl =>
        $"./_content/PanoramicData.Blazor/js/common.js?v={Version.AssemblyFileVersion}";
}
