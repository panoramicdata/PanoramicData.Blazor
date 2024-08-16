using PanoramicData.Blazor.Models.Monaco;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDMonaco
{
	private string _theme = "vs";
	private string _themePreference = "light";
	private string _language = "sql";
	private string _value = "SELECT 10 * 10\n  FROM [Temp]";

	//private PDMonacoEditor? _monacoEditor;

	private void AddMethods(MethodCache cache)
	{
		var descriptionProvider = new DefaultDescriptionProvider();
		cache.AddPublicStaticMethods("ncalc", typeof(Math), descriptionProvider); // description comes from provider
		cache.AddPublicStaticMethods("ncalc", typeof(CustomMethods)); // description specified in attributes
	}

	private void OnSetLanguage(string language)
	{
		_language = language;
		_value = _language switch
		{
			"ncalc" => "10 * -3.14 + Sqrt(9) + FormatDate(#27/08/2010#, 'yyyy-MM-dd') + Round(Pow([Pi], 2) + Pow([Pi2], 2) + [X], 2)",
			"rmscript" => "[DateTime: value={QueryEndDate}, addmonths=-12, format=yyyy-MM-15, storeAsHidden=QueryStartDate]",
			"javascript" => "if(Math.PI() > 3) {\n   this.setError(\"Invalid Function\");\n}",
			_ => "SELECT 10 * 10\n  FROM [Temp]"
		};
		StateHasChanged();
	}

	private void OnSetTheme(string themePreference)
	{
		//if (_monacoEditor != null)
		//{
		_theme = _language switch
		{
			"rmscript" => themePreference == "light" ? "rm-light" : "rm-dark",
			"ncalc" => themePreference == "light" ? "ncalc-light" : "ncalc-dark",
			_ => themePreference == "light" ? "vs" : "vs-dark"
		};
		_themePreference = themePreference;
		//}
	}
}
