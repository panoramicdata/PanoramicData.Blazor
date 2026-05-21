using BlazorMonaco;
using BlazorMonaco.Languages;
using PanoramicData.Blazor.Models.Monaco;
using Range = BlazorMonaco.Range;

namespace PanoramicData.Blazor;

/// <summary>
/// Hosts a Monaco editor instance with optional language registration, completion support, and value synchronization.
/// </summary>
public partial class PDMonacoEditor : IAsyncDisposable
{
	private static int _seq;

	private IJSObjectReference? _module;
	private string _theme = string.Empty;
	private StandaloneCodeEditor? _monacoEditor;
	private DotNetObjectReference<PDMonacoEditor>? _objRef;
	private static readonly MethodCache _methodCache = new();

	/// <summary>
	/// Gets or sets the JavaScript runtime used to initialize and interact with Monaco.
	/// </summary>
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

	/// <summary>
	/// Applies one or more edit operations to the current editor model.
	/// </summary>
	/// <param name="source">An identifier describing the source of the edits.</param>
	/// <param name="edits">The edit operations to execute.</param>
	/// <param name="endCursorState">Optional cursor selections to apply after edits complete.</param>
	public async Task ExecuteEdits(string source, List<IdentifiedSingleEditOperation> edits, List<Selection>? endCursorState = null)
	{
		if (_monacoEditor != null)
		{
			await _monacoEditor.ExecuteEdits(source, edits, endCursorState ?? []).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Returns completion items for the provided function context.
	/// </summary>
	/// <param name="range">The range where completion is requested.</param>
	/// <param name="functionName">The function name used to resolve completion candidates.</param>
	/// <returns>The completion items available for the current language and function.</returns>
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

	/// <summary>
	/// Gets the full editor value using the specified end-of-line and BOM options.
	/// </summary>
	/// <param name="endOfLinePreference">The end-of-line representation to use in the returned text.</param>
	/// <param name="preserveBOM">True to preserve a byte order mark when available; otherwise false.</param>
	/// <returns>The editor text, or an empty string when the editor is not initialized.</returns>
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

	/// <summary>
	/// Gets editor text for a specific range.
	/// </summary>
	/// <param name="range">The model range to read.</param>
	/// <param name="endOfLinePreference">The end-of-line representation to use in the returned text.</param>
	/// <returns>The text in the supplied range, or an empty string when the editor is not initialized.</returns>
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

	/// <summary>
	/// Gets the current editor selection.
	/// </summary>
	/// <returns>The current selection, or null when the editor is not initialized.</returns>
	public async Task<Selection?> GetSelection()
	{
		if (_monacoEditor != null)
		{
			return await _monacoEditor.GetSelection().ConfigureAwait(true);
		}

		return null;
	}

	/// <summary>
	/// Returns signature help entries for the provided function.
	/// </summary>
	/// <param name="functionName">The function name used to resolve signature information.</param>
	/// <returns>Signature information entries for the current language and function.</returns>
	[JSInvokable]
	public SignatureInformation[] GetSignatures(string functionName)
		=> ShowSuggestions ? [.. _methodCache.GetSignatures(Language, functionName)] : [];

	/// <summary>
	/// Initializes Monaco integration and custom language registrations after first render.
	/// </summary>
	/// <param name="firstRender">True on first render; otherwise false.</param>
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime != null)
		{
			_objRef = DotNetObjectReference.Create(this);
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDMonacoEditor.razor.js").ConfigureAwait(true);
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

	/// <summary>
	/// Applies parameter-driven updates such as theme changes to the underlying editor.
	/// </summary>
	protected override async Task OnParametersSetAsync()
	{
		try
		{
			if (_monacoEditor != null)
			{
				// Always update the theme if it has changed, even if it's being set for the first time
				if (!string.IsNullOrEmpty(Theme) && Theme != _theme)
				{
					_theme = Theme;
					await _monacoEditor.UpdateOptions(new EditorUpdateOptions { Theme = _theme });
				}
			}
			else if (!string.IsNullOrEmpty(Theme))
			{
				// Store the theme even if the editor isn't initialized yet
				_theme = Theme;
			}
		}
		catch (JSException)
		{
			// This can happen if the editor is disposed before the parameters are set.
			// We can safely ignore this exception.
		}
	}

	/// <summary>
	/// Resolves and caches completion metadata for the requested method.
	/// </summary>
	/// <param name="methodName">The method name to resolve.</param>
	[JSInvokable]
	public async Task ResolveCompletionAsync(string methodName)
	{
		if (UpdateCacheAsync != null)
		{
			await UpdateCacheAsync.Invoke(_methodCache, Language, methodName);
		}
	}

	/// <summary>
	/// Replaces the entire editor value.
	/// </summary>
	/// <param name="value">The text to set in the editor.</param>
	public async Task SetMonacoValueAsync(string value)
	{
		if (_monacoEditor != null)
		{
			var model = await _monacoEditor.GetModel();
			await model.SetValue(value);
		}
	}

	/// <summary>
	/// Sets the editor selection.
	/// </summary>
	/// <param name="selection">The selection to apply.</param>
	/// <param name="source">An optional source identifier for the selection change.</param>
	public async Task SetSelectionAsync(Selection selection, string source = "")
	{
		if (_monacoEditor != null)
		{
			await _monacoEditor.SetSelection(selection, source);
		}
	}

	/// <summary>
	/// Updates editor options on the live Monaco instance.
	/// </summary>
	/// <param name="options">The option values to apply.</param>
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

	/// <summary>
	/// Releases JavaScript references and editor state held by this component.
	/// </summary>
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
