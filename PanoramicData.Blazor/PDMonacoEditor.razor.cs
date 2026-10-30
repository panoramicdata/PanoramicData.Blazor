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
	private const int _monacoLoadTimeoutMs = 15000;

	// The BlazorMonaco editor is only rendered once monaco exists: creating it earlier throws inside BlazorMonaco,
	// where nothing can catch it, and takes the host's circuit down.
	private bool _isMonacoReady;
	private bool _monacoFailedToLoad;
	private string? _pendingValue;

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
	public Task ExecuteEdits(string source, List<IdentifiedSingleEditOperation> edits)
		=> ExecuteEdits(source, edits, null);

	/// <summary>
	/// Applies one or more edit operations to the current editor model.
	/// </summary>
	/// <param name="source">An identifier describing the source of the edits.</param>
	/// <param name="edits">The edit operations to execute.</param>
	/// <param name="endCursorState">Cursor selections to apply after edits complete, or null for none.</param>
	public Task ExecuteEdits(string source, List<IdentifiedSingleEditOperation> edits, List<Selection>? endCursorState)
		=> RunOnEditorAsync(editor => editor.ExecuteEdits(source, edits, endCursorState ?? []));

	// Every call into the browser fails once it has gone (a closed tab, a dropped connection, a disposed editor, or
	// prerendering before it exists). Such a failure must never reach the host; anything else is a real fault.
	private static bool IsBrowserUnavailable(Exception ex)
		=> ex is JSException or JSDisconnectedException or OperationCanceledException or ObjectDisposedException or InvalidOperationException;

	private async Task RunOnEditorAsync(Func<StandaloneCodeEditor, Task> action)
	{
		if (_monacoEditor is { } editor)
		{
			try
			{
				await action(editor).ConfigureAwait(true);
			}
			catch (Exception ex) when (IsBrowserUnavailable(ex))
			{
				// See IsBrowserUnavailable.
			}
		}
	}

	private async Task<T> RunOnEditorAsync<T>(Func<StandaloneCodeEditor, Task<T>> action, T fallback)
	{
		if (_monacoEditor is { } editor)
		{
			try
			{
				return await action(editor).ConfigureAwait(true);
			}
			catch (Exception ex) when (IsBrowserUnavailable(ex))
			{
				// See IsBrowserUnavailable.
			}
		}

		return fallback;
	}

	private async Task RunOnModuleAsync(string identifier, params object?[] args)
	{
		if (_module is { } module)
		{
			try
			{
				await module.InvokeVoidAsync(identifier, args).ConfigureAwait(true);
			}
			catch (Exception ex) when (IsBrowserUnavailable(ex))
			{
				// See IsBrowserUnavailable.
			}
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

	private StandaloneEditorConstructionOptions GetOptions()
	{
		var options = new StandaloneEditorConstructionOptions
		{
			AutomaticLayout = true,
			Language = Language,
			Theme = Theme,
			Value = _pendingValue ?? Value
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
	public Task<string> GetMonacoValueAsync(EndOfLinePreference endOfLinePreference, bool preserveBOM)
		=> RunOnEditorAsync(async editor => await (await editor.GetModel()).GetValue(endOfLinePreference, preserveBOM), string.Empty);

	/// <summary>
	/// Gets editor text for a specific range.
	/// </summary>
	/// <param name="range">The model range to read.</param>
	/// <param name="endOfLinePreference">The end-of-line representation to use in the returned text.</param>
	/// <returns>The text in the supplied range, or an empty string when the editor is not initialized.</returns>
	public Task<string> GetMonacoValueAsync(Range range, EndOfLinePreference endOfLinePreference)
		=> RunOnEditorAsync(async editor => await (await editor.GetModel()).GetValueInRange(range, endOfLinePreference), string.Empty);

	/// <summary>
	/// Gets the current editor selection.
	/// </summary>
	/// <returns>The current selection, or null when the editor is not initialized.</returns>
	public Task<Selection?> GetSelection()
		=> RunOnEditorAsync<Selection?>(async editor => await editor.GetSelection(), null);

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
			try
			{
				await PrepareMonacoAsync(JSRuntime).ConfigureAwait(true);
			}
			catch (Exception ex) when (IsBrowserUnavailable(ex))
			{
				_monacoFailedToLoad = true;
			}

			StateHasChanged();
		}
	}

	// Waits for monaco, then sets up languages (and any custom themes a host defines with them) before the editor exists.
	private async Task PrepareMonacoAsync(IJSRuntime jsRuntime)
	{
		_objRef = DotNetObjectReference.Create(this);
		_module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDMonacoEditor.razor.js").ConfigureAwait(true);
		if (_module is null)
		{
			return;
		}

		if (!await _module.InvokeAsync<bool>("waitForMonaco", _monacoLoadTimeoutMs).ConfigureAwait(true))
		{
			_monacoFailedToLoad = true;
			return;
		}

		await _module.InvokeVoidAsync("initialize", _objRef);
		await RegisterCustomLanguagesAsync(_module).ConfigureAwait(true);

		InitializeCache?.Invoke(_methodCache);

		if (InitializeCacheAsync != null)
		{
			await InitializeCacheAsync(_methodCache).ConfigureAwait(true);
		}

		_isMonacoReady = true;
	}

	// Allows custom languages to be registered, initialising each one the script accepts.
	private async Task RegisterCustomLanguagesAsync(IJSObjectReference module)
	{
		var languages = new List<Language>();
		RegisterLanguages?.Invoke(languages);
		foreach (var language in languages)
		{
			var registered = await module.InvokeAsync<bool>("registerLanguage", language.Id, language);
			if (registered)
			{
				InitializeLanguage?.Invoke(language);

				if (InitializeLanguageAsync != null)
				{
					await InitializeLanguageAsync(language).ConfigureAwait(true);
				}
			}
		}
	}

	private Task OnMonacoEditorBlurAsync()
		=> UpdateValueOnBlur ? ReportValueAsync() : Task.CompletedTask;

	private Task OnMonacoEditorContentChangedAsync()
		=> UpdateValueOnBlur ? Task.CompletedTask : ReportValueAsync();

	private async Task ReportValueAsync()
	{
		if (_monacoEditor is null)
		{
			return;
		}

		string value;
		try
		{
			value = await (await _monacoEditor.GetModel()).GetValue(EndOfLinePreference.LF, true);
		}
		catch (Exception ex) when (IsBrowserUnavailable(ex))
		{
			return;
		}

		await ValueChanged.InvokeAsync(value);
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
		catch (Exception ex) when (IsBrowserUnavailable(ex))
		{
			// See IsBrowserUnavailable.
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
	public Task SetMonacoValueAsync(string value)
	{
		if (_monacoEditor is null)
		{
			// Used when the editor is created, so a value set before then is not lost.
			_pendingValue = value;
			return Task.CompletedTask;
		}

		return RunOnEditorAsync(async editor => await (await editor.GetModel()).SetValue(value));
	}

	/// <summary>
	/// Inserts text at the cursor, replacing any selected text, and returns focus to the editor.
	/// </summary>
	/// <param name="text">The text to insert.</param>
	public Task InsertTextAsync(string text)
		=> RunOnEditorAsync(async editor =>
		{
			if (await editor.GetSelection() is { } selection)
			{
				await editor.ExecuteEdits(
					"pd-monaco-editor",
					[new IdentifiedSingleEditOperation { Range = selection, Text = text, ForceMoveMarkers = true }],
					[]);
				await editor.Focus();
			}
		});

	/// <summary>
	/// Sets the editor selection.
	/// </summary>
	/// <param name="selection">The selection to apply.</param>
	public Task SetSelectionAsync(Selection selection) => SetSelectionAsync(selection, string.Empty);

	/// <summary>
	/// Sets the editor selection.
	/// </summary>
	/// <param name="selection">The selection to apply.</param>
	/// <param name="source">A source identifier for the selection change.</param>
	public Task SetSelectionAsync(Selection selection, string source)
		=> RunOnEditorAsync(editor => editor.SetSelection(selection, source));

	/// <summary>
	/// Updates editor options on the live Monaco instance.
	/// </summary>
	/// <param name="options">The option values to apply.</param>
	public Task UpdateOptions(EditorUpdateOptions options)
		=> RunOnEditorAsync(editor => editor.UpdateOptions(options));

	/// <summary>
	/// Forces the Monaco editor to recalculate its layout.
	/// This should be called when the container size changes.
	/// </summary>
	public Task ForceLayoutUpdateAsync()
		=> RunOnEditorAsync(editor => editor.Layout());

	/// <summary>
	/// Disables a specific key binding combination in the Monaco editor.
	/// </summary>
	/// <param name="keyCode">The key code (e.g., 13 for Enter)</param>
	public Task DisableKeyBindingAsync(int keyCode) => DisableKeyBindingAsync(keyCode, false, false, false);

	/// <summary>
	/// Disables a specific key binding combination in the Monaco editor.
	/// </summary>
	/// <param name="keyCode">The key code (e.g., 13 for Enter)</param>
	/// <param name="ctrlKey">Whether Ctrl key is required</param>
	public Task DisableKeyBindingAsync(int keyCode, bool ctrlKey) => DisableKeyBindingAsync(keyCode, ctrlKey, false, false);

	/// <summary>
	/// Disables a specific key binding combination in the Monaco editor.
	/// </summary>
	/// <param name="keyCode">The key code (e.g., 13 for Enter)</param>
	/// <param name="ctrlKey">Whether Ctrl key is required</param>
	/// <param name="altKey">Whether Alt key is required</param>
	public Task DisableKeyBindingAsync(int keyCode, bool ctrlKey, bool altKey) => DisableKeyBindingAsync(keyCode, ctrlKey, altKey, false);

	/// <summary>
	/// Disables a specific key binding combination in the Monaco editor.
	/// </summary>
	/// <param name="keyCode">The key code (e.g., 13 for Enter)</param>
	/// <param name="ctrlKey">Whether Ctrl key is required</param>
	/// <param name="altKey">Whether Alt key is required</param>
	/// <param name="shiftKey">Whether Shift key is required</param>
	public Task DisableKeyBindingAsync(int keyCode, bool ctrlKey, bool altKey, bool shiftKey)
		=> RunOnModuleAsync("disableKeyBinding", keyCode, ctrlKey, altKey, shiftKey);

	/// <summary>
	/// Enables a previously disabled key binding combination in the Monaco editor.
	/// </summary>
	/// <param name="keyCode">The key code (e.g., 13 for Enter)</param>
	public Task EnableKeyBindingAsync(int keyCode) => EnableKeyBindingAsync(keyCode, false, false, false);

	/// <summary>
	/// Enables a previously disabled key binding combination in the Monaco editor.
	/// </summary>
	/// <param name="keyCode">The key code (e.g., 13 for Enter)</param>
	/// <param name="ctrlKey">Whether Ctrl key is required</param>
	public Task EnableKeyBindingAsync(int keyCode, bool ctrlKey) => EnableKeyBindingAsync(keyCode, ctrlKey, false, false);

	/// <summary>
	/// Enables a previously disabled key binding combination in the Monaco editor.
	/// </summary>
	/// <param name="keyCode">The key code (e.g., 13 for Enter)</param>
	/// <param name="ctrlKey">Whether Ctrl key is required</param>
	/// <param name="altKey">Whether Alt key is required</param>
	public Task EnableKeyBindingAsync(int keyCode, bool ctrlKey, bool altKey) => EnableKeyBindingAsync(keyCode, ctrlKey, altKey, false);

	/// <summary>
	/// Enables a previously disabled key binding combination in the Monaco editor.
	/// </summary>
	/// <param name="keyCode">The key code (e.g., 13 for Enter)</param>
	/// <param name="ctrlKey">Whether Ctrl key is required</param>
	/// <param name="altKey">Whether Alt key is required</param>
	/// <param name="shiftKey">Whether Shift key is required</param>
	public Task EnableKeyBindingAsync(int keyCode, bool ctrlKey, bool altKey, bool shiftKey)
		=> RunOnModuleAsync("enableKeyBinding", keyCode, ctrlKey, altKey, shiftKey);

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
