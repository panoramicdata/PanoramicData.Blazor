using BlazorMonaco;
using BlazorMonaco.Languages;
using PanoramicData.Blazor.Models.Monaco;
using Range = BlazorMonaco.Range;

namespace PanoramicData.Blazor;

public partial class PDMonacoEditor : IAsyncDisposable
{
	private static int _seq;

	private IJSObjectReference? _module;
	private string _theme = string.Empty;
	private StandaloneCodeEditor? _monacoEditor;
	private DotNetObjectReference<PDMonacoEditor>? _objRef;
	private static readonly MethodCache _methodCache = new();

	[Inject]
	public IJSRuntime? JSRuntime { get; set; }

	/// <summary>
	/// Gets or sets the unique identifier for the editor.
	/// </summary>
	[Parameter]
	public string Id { get; set; } = $"PDMonacoEditor-{++_seq}";

	/// <summary>
	/// Gets or sets the programming language for the editor.
	/// </summary>
	[Parameter]
	public string Language { get; set; } = "javascript";

	/// <summary>
	/// Gets or sets whether to show code suggestions.
	/// </summary>
	[Parameter]
	public bool ShowSuggestions { get; set; } = true;

	/// <summary>
	/// Gets or sets the theme for the editor.
	/// </summary>
	[Parameter]
	public string Theme { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the content of the editor.
	/// </summary>
	[Parameter]
	public string Value { get; set; } = string.Empty;

	/// <summary>
	/// An event callback that is invoked when the content of the editor changes.
	/// </summary>
	[Parameter]
	public EventCallback<string> ValueChanged { get; set; }

	/// <summary>
	/// Gets or sets whether the Value parameter is updated only when the editor loses focus.
	/// </summary>
	[Parameter]
	public bool UpdateValueOnBlur { get; set; }

	/// <summary>
	/// An action to initialize the method cache for language completions.
	/// </summary>
	[Parameter]
	public Action<MethodCache>? InitializeCache { get; set; }

	/// <summary>
	/// An async function to initialize the method cache for language completions.
	/// </summary>
	[Parameter]
	public Func<MethodCache, Task>? InitializeCacheAsync { get; set; }

	/// <summary>
	/// An action to initialize the editor options.
	/// </summary>
	[Parameter]
	public Action<StandaloneEditorConstructionOptions>? InitializeOptions { get; set; }

	/// <summary>
	/// An action to initialize a custom language.
	/// </summary>
	[Parameter]
	public Action<Language>? InitializeLanguage { get; set; }

	/// <summary>
	/// An async function to initialize a custom language.
	/// </summary>
	[Parameter]
	public Func<Language, Task>? InitializeLanguageAsync { get; set; }

	/// <summary>
	/// An action to register custom languages.
	/// </summary>
	[Parameter]
	public Action<List<Language>>? RegisterLanguages { get; set; }

	/// <summary>
	/// An async function to update the method cache.
	/// </summary>
	[Parameter]
	public Func<MethodCache, string, string, Task>? UpdateCacheAsync { get; set; }

	/// <summary>
	/// An event callback that is invoked when the selection changes in the editor.
	/// </summary>
	[Parameter]
	public EventCallback<Selection> SelectionChanged { get; set; }

	public async Task ExecuteEdits(string source, List<IdentifiedSingleEditOperation> edits, List<Selection>? endCursorState = null)
	{
		if (_monacoEditor != null)
		{
			await _monacoEditor.ExecuteEdits(source, edits, endCursorState ?? []).ConfigureAwait(true);
		}
	}

	[JSInvokable]
	public CompletionItem[] GetCompletions(Range range, string functionName)
		=> ShowSuggestions ? [.. _methodCache.GetCompletionItems(Language, functionName)] : [];

	private StandaloneEditorConstructionOptions GetOptions(StandaloneCodeEditor editor)
	{
		var options = new StandaloneEditorConstructionOptions
		{
			AutomaticLayout = true,
			Language = Language,
			Theme = Theme,
			Value = Value
		};
		InitializeOptions?.Invoke(options);
		return options;
	}

	public async Task<string> GetMonacoValueAsync(EndOfLinePreference endOfLinePreference, bool preserveBOM)
	{
		if (_monacoEditor != null)
		{
			var model = await _monacoEditor.GetModel();
			var value = await model.GetValue(endOfLinePreference, preserveBOM);
			return value;
		}

		return string.Empty;
	}

	public async Task<string> GetMonacoValueAsync(Range range, EndOfLinePreference endOfLinePreference)
	{
		if (_monacoEditor != null)
		{
			var model = await _monacoEditor.GetModel();
			var value = await model.GetValueInRange(range, endOfLinePreference);
			return value;
		}

		return string.Empty;
	}

	public async Task<Selection?> GetSelection()
	{
		if (_monacoEditor != null)
		{
			return await _monacoEditor.GetSelection().ConfigureAwait(true);
		}

		return null;
	}

	[JSInvokable]
	public SignatureInformation[] GetSignatures(string functionName)
		=> ShowSuggestions ? [.. _methodCache.GetSignatures(Language, functionName)] : [];

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime != null)
		{
			_objRef = DotNetObjectReference.Create(this);
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDMonacoEditor.razor.js").ConfigureAwait(true); ;
			if (_module != null)
			{
				await _module.InvokeVoidAsync("initialize", _objRef);

				// allow custom languages to be registered
				var languages = new List<Language>();
				RegisterLanguages?.Invoke(languages);
				foreach (var language in languages)
				{
					var registered = await _module.InvokeAsync<bool>("registerLanguage", language.Id, language);
					if (registered)
					{
						InitializeLanguage?.Invoke(language);

						if (InitializeLanguageAsync != null)
						{
							await InitializeLanguageAsync(language).ConfigureAwait(true);
						}
					}
				}

				InitializeCache?.Invoke(_methodCache);

				if (InitializeCacheAsync != null)
				{
					await InitializeCacheAsync(_methodCache).ConfigureAwait(true);
				}
			}
		}
	}

	private async Task OnMonacoEditorBlurAsync()
	{
		if (_monacoEditor != null && UpdateValueOnBlur)
		{
			var model = await _monacoEditor.GetModel();
			var value = await model.GetValue(EndOfLinePreference.LF, true);
			await ValueChanged.InvokeAsync(value);
		}
	}

	private async Task OnMonacoEditorContentChangedAsync(ModelContentChangedEvent args)
	{
		if (_monacoEditor != null && !UpdateValueOnBlur)
		{
			var model = await _monacoEditor.GetModel();
			var value = await model.GetValue(EndOfLinePreference.LF, true);
			await ValueChanged.InvokeAsync(value);
		}
	}

	private Task OnMonacoEditorSelectionChangeAsync(CursorSelectionChangedEvent evt)
	{
		return SelectionChanged.InvokeAsync(evt.Selection);
	}

	protected override async Task OnParametersSetAsync()
	{
		try
		{
			if (_monacoEditor != null)
			{
				if (Theme != _theme)
				{
					_theme = Theme;
					await _monacoEditor.UpdateOptions(new EditorUpdateOptions { Theme = _theme });
				}
			}
		}
		catch (JSException)
		{
			// This can happen if the editor is disposed before the parameters are set.
			// We can safely ignore this exception.
		}
	}

	[JSInvokable]
	public async Task ResolveCompletionAsync(string methodName)
	{
		if (UpdateCacheAsync != null)
		{
			await UpdateCacheAsync.Invoke(_methodCache, Language, methodName);
		}
	}

	public async Task SetMonacoValueAsync(string value)
	{
		if (_monacoEditor != null)
		{
			var model = await _monacoEditor.GetModel();
			await model.SetValue(value);
		}
	}

	public async Task SetSelectionAsync(Selection selection, string source = "")
	{
		if (_monacoEditor != null)
		{
			await _monacoEditor.SetSelection(selection, source);
		}
	}


	public async Task UpdateOptions(EditorUpdateOptions options)
	{
		if (_monacoEditor != null)
		{
			await _monacoEditor.UpdateOptions(options).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Forces the Monaco editor to recalculate its layout.
	/// This should be called when the container size changes.
	/// </summary>
	public async Task ForceLayoutUpdateAsync()
	{
		if (_monacoEditor != null)
		{
			await _monacoEditor.Layout();
		}
	}

	/// <summary>
	/// Disables a specific key binding combination in the Monaco editor.
	/// </summary>
	/// <param name="keyCode">The key code (e.g., 13 for Enter)</param>
	/// <param name="ctrlKey">Whether Ctrl key is required</param>
	/// <param name="altKey">Whether Alt key is required</param>
	/// <param name="shiftKey">Whether Shift key is required</param>
	public async Task DisableKeyBindingAsync(int keyCode, bool ctrlKey = false, bool altKey = false, bool shiftKey = false)
	{
		if (_module != null)
		{
			await _module.InvokeVoidAsync("disableKeyBinding", keyCode, ctrlKey, altKey, shiftKey);
		}
	}

	/// <summary>
	/// Enables a previously disabled key binding combination in the Monaco editor.
	/// </summary>
	/// <param name="keyCode">The key code (e.g., 13 for Enter)</param>
	/// <param name="ctrlKey">Whether Ctrl key is required</param>
	/// <param name="altKey">Whether Alt key is required</param>
	/// <param name="shiftKey">Whether Shift key is required</param>
	public async Task EnableKeyBindingAsync(int keyCode, bool ctrlKey = false, bool altKey = false, bool shiftKey = false)
	{
		if (_module != null)
		{
			await _module.InvokeVoidAsync("enableKeyBinding", keyCode, ctrlKey, altKey, shiftKey);
		}
	}

	#region IAsyncDisposable

	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);

			_objRef?.Dispose();

			if (_module != null)
			{
				await _module.DisposeAsync().ConfigureAwait(true);
			}
		}
		catch
		{
			// Ignore any exceptions during disposal
			// This can happen if the module is already disposed or if there are issues with the JS runtime.
		}
		finally
		{
			_module = null;
			_monacoEditor = null;
			_objRef = null;
			_theme = string.Empty;
		}
	}

	#endregion
}
