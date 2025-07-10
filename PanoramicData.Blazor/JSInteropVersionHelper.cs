namespace PanoramicData.Blazor;

public static class JSInteropVersionHelper
{
    public static string CommonJsUrl =>
        $"./_content/PanoramicData.Blazor/js/common.js?v={Version.AssemblyFileVersion}";
}
