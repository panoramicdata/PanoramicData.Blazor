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

	[Parameter]
	public string Id { get; set; } = $"PDMonacoEditor-{++_seq}";

	[Parameter]
	public string Language { get; set; } = "javascript";

	[Parameter]
	public bool ShowSuggestions { get; set; } = true;

	[Parameter]
	public string Theme { get; set; } = string.Empty;

	[Parameter]
	public string Value { get; set; } = string.Empty;

	[Parameter]
	public EventCallback<string> ValueChanged { get; set; }

	[Parameter]
	public bool UpdateValueOnBlur { get; set; }

	[Parameter]
	public Action<MethodCache>? InitializeCache { get; set; }

	[Parameter]
	public Func<MethodCache, Task>? InitializeCacheAsync { get; set; }

	[Parameter]
	public Action<StandaloneEditorConstructionOptions>? InitializeOptions { get; set; }

	[Parameter]
	public Action<Language>? InitializeLanguage { get; set; }

	[Parameter]
	public Func<Language, Task>? InitializeLanguageAsync { get; set; }

	[Parameter]
	public Action<List<Language>>? RegisterLanguages { get; set; }

	[Parameter]
	public Func<MethodCache, string, string, Task>? UpdateCacheAsync { get; set; }

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
			var value = await model.GetValue(EndOfLinePreference.CRLF, true);
			await ValueChanged.InvokeAsync(value);
		}
	}

	private async Task OnMonacoEditorKeyUpAsync(KeyboardEvent evt)
	{
		if (_monacoEditor != null && !UpdateValueOnBlur)
		{
			var model = await _monacoEditor.GetModel();
			var value = await model.GetValue(EndOfLinePreference.CRLF, true);
			await ValueChanged.InvokeAsync(value);
		}
	}

	private Task OnMonacoEditorSelectionChangeAsync(CursorSelectionChangedEvent evt)
	{
		return SelectionChanged.InvokeAsync(evt.Selection);
	}

	protected override async Task OnParametersSetAsync()
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
		}
	}

	#endregion
}
