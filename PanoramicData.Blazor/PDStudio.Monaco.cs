using PanoramicData.Blazor.Models.Monaco;

namespace PanoramicData.Blazor;

/// <summary>
/// The callbacks a host uses to configure the Monaco editor.
/// </summary>
public partial class PDStudio
{
	/// <summary>
	/// Gets or sets a callback to initialize Monaco editor options.
	/// </summary>
	[Parameter] public Action<StandaloneEditorConstructionOptions>? InitializeMonacoOptions { get; set; }

	/// <summary>
	/// Gets or sets a callback to initialize the method cache for language completions.
	/// </summary>
	[Parameter] public Action<MethodCache>? InitializeMethodCache { get; set; }

	/// <summary>
	/// Gets or sets a callback to register custom languages for Monaco editor.
	/// </summary>
	[Parameter] public Action<List<Language>>? RegisterLanguages { get; set; }

	/// <summary>
	/// Gets or sets a callback to initialize custom language configurations.
	/// </summary>
	[Parameter] public Func<Language, Task>? InitializeLanguageAsync { get; set; }

	/// <summary>
	/// Gets or sets a callback to update method cache asynchronously.
	/// </summary>
	[Parameter] public Func<MethodCache, string, string, Task>? UpdateMethodCacheAsync { get; set; }

	/// <summary>
	/// Gets a Monaco initializer function that combines default and custom options.
	/// </summary>
	private void GetMonacoInitializer(StandaloneEditorConstructionOptions options)
	{
		// Apply default PDStudio Monaco configuration
		ConfigureMonacoOptions(options);

		// Apply custom configuration if provided
		InitializeMonacoOptions?.Invoke(options);
	}

	/// <summary>
	/// Configures Monaco editor options to ensure proper sizing within PDStudio.
	/// </summary>
	private static void ConfigureMonacoOptions(StandaloneEditorConstructionOptions options)
	{
		// Ensure the editor uses automatic layout and fills its container
		options.AutomaticLayout = true;
		options.ScrollBeyondLastLine = false;
		options.WordWrap = "on";
		options.Minimap = new EditorMinimapOptions { Enabled = false };

		// Room between the line under the toolbar and the first line of code
		options.Padding = new EditorPaddingOptions { Top = 6 };

		// Enable IntelliSense and syntax highlighting features by default
		options.LineNumbers = "on";
		options.Suggest = new SuggestOptions
		{
			ShowWords = false // Don't show generic word suggestions, prefer language-specific ones
		};

		// Enable folding and bracket matching for better code editing experience
		options.Folding = true;
		options.MatchBrackets = "always";

		// Configure scrolling behaviour for better user experience
		options.SmoothScrolling = true;
		options.MouseWheelScrollSensitivity = 1;
	}
}
