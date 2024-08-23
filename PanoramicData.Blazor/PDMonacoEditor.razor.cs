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

	[JSInvokable]
	public CompletionItem[] GetCompletions(BlazorMonaco.Range range)
		=> _methodCache.GetCompletionItems(Language).ToArray();

	[JSInvokable]
	public SignatureInformation[] GetSignatures(string functionName)
		=> _methodCache.GetSignatures(Language, functionName).ToArray();

	private StandaloneEditorConstructionOptions GetOptions(StandaloneCodeEditor editor)
	{
		return new StandaloneEditorConstructionOptions
		{
			AutomaticLayout = true,
			Language = Language,
			Theme = Theme,
			Value = Value
		};
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			if (JSRuntime != null)
			{
				_objRef = DotNetObjectReference.Create(this);
				_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDMonacoEditor.razor.js").ConfigureAwait(true); ;
				if (_module != null)
				{
					await _module.InvokeVoidAsync("configureMonaco", _objRef);
				}
			}

			// cache language methods
			if (InitializeCache != null)
			{
				InitializeCache(_methodCache);
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
