using BlazorMonaco;
using BlazorMonaco.Languages;
using PanoramicData.Blazor.Models.Monaco;

namespace PanoramicData.Blazor;

public partial class PDMonacoEditor : IAsyncDisposable
{
	private static int _seq;

	private IJSObjectReference? _module;
	private string _theme = string.Empty;
	private string _language = "javascript";
	private StandaloneCodeEditor? _monacoEditor;
	private readonly MethodCache _methodCache = new();
	private DotNetObjectReference<PDMonacoEditor>? _objRef;

	[Inject]
	public IJSRuntime? JSRuntime { get; set; }

	[Parameter]
	public string Id { get; set; } = $"PDMonacoEditor-{++_seq}";

	[Parameter]
	public string Language { get; set; } = "javascript";

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
	public Action<StandaloneEditorConstructionOptions>? InitializeOptions { get; set; }

	[Parameter]
	public Func<Language, Task>? InitializeLanguageAsync { get; set; }

	[Parameter]
	public Action<List<Language>>? RegisterLanguages { get; set; }


	public async Task ExecuteEdits(string source, List<IdentifiedSingleEditOperation> edits, List<Selection>? endCursorState = null)
	{
		if (_monacoEditor != null)
		{
			await _monacoEditor.ExecuteEdits(source, edits, endCursorState ?? []).ConfigureAwait(true);
		}
	}

	[JSInvokable]
	public CompletionItem[] GetCompletions(BlazorMonaco.Range range)
		=> _methodCache.GetCompletionItems(Language).ToArray();

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
		=> _methodCache.GetSignatures(Language, functionName).ToArray();

	private StandaloneEditorConstructionOptions GetOptions(StandaloneCodeEditor editor)
	{
		var options = new StandaloneEditorConstructionOptions
		{
			AutomaticLayout = true,
			Language = Language,
			Theme = Theme,
			Value = Value
		};
		if (InitializeOptions != null)
		{
			InitializeOptions(options);
		}
		return options;
	}

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
					var registered = await _module.InvokeAsync<bool>("registerLanguage", language.Id, language.ShowCompletions, language.SignatureHelpTriggers);
					if (registered && InitializeLanguageAsync != null)
					{
						await InitializeLanguageAsync(language);
					}
				}
				InitializeCache?.Invoke(_methodCache);
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
			if (_objRef != null)
			{
				_objRef.Dispose();
			}
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
