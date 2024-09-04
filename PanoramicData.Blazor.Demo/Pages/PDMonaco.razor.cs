using PanoramicData.Blazor.Models.Monaco;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDMonaco
{
	private string _theme = "vs";
	private string _themePreference = "light";
	private string _language = "sql";
	private string _value = "SELECT 10 * 10\n  FROM [Temp]";

	private void InitializeCache(MethodCache cache)
	{
		//cache.Options.IncludeMethodTypeName = true;
		cache.AddPublicStaticTypeMethods("ncalc", typeof(Math), new DefaultDescriptionProvider());
		cache.AddTypeMethods("ncalc", typeof(NCalcExtensions.Extensions.IFunctionPrototypes));
	}

	private void OnSetLanguage(string language)
	{
		_language = language;
		_value = _language switch
		{
			"ncalc" => "10 * -3.14 + Sqrt(9)",
			"javascript" => "if(Math.PI() > 3) {\n   this.setError(\"Invalid Function\");\n}",
			_ => "SELECT 10 * 10\n  FROM [Temp]"
		};
		StateHasChanged();
	}

	private void OnSetTheme(string themePreference)
	{
		_theme = _language switch
		{
			"ncalc" => themePreference == "light" ? "ncalc-light" : "ncalc-dark",
			_ => themePreference == "light" ? "vs" : "vs-dark"
		};
		_themePreference = themePreference;
	}
}
