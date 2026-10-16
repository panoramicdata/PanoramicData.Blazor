using BlazorMonaco;
using BlazorMonaco.Editor;
using PanoramicData.Blazor.Models.Monaco;

namespace PanoramicData.Blazor;

/// <summary>
/// A read-only (by default) Monaco diff view of two texts. Changing <see cref="Original"/> or
/// <see cref="Modified"/> swaps text into the existing models, so scroll position survives live refresh.
/// </summary>
public partial class PDDiffPanel : IAsyncDisposable
{
	private static int _seq;

	private StandaloneDiffEditor? _editor;
	private TextModel? _originalModel;
	private TextModel? _modifiedModel;
	private bool _initialized;
	private string _shownOriginal = string.Empty;
	private string _shownModified = string.Empty;
	private string _shownLanguage = string.Empty;
	private string _shownTheme = string.Empty;
	private bool _sideBySide = true;
	private bool _lastParamSideBySide = true;
	private bool _sideBySideSeen;
	private bool _shownSideBySide = true;
	private bool _shownReadOnly = true;
	private bool _disposed;
	private int _editorGeneration;
	private int _initAttempts;
	private bool _editorCreated;
	private IJSObjectReference? _module;
	private bool _loadFailed;

	// Base delay before recreating the editor after a failed init; doubles per attempt (capped at 16x) and is a
	// field so tests can shorten it. With the default 300ms and 9 attempts the total budget is about 30 seconds.
	private TimeSpan _initRetryDelay = TimeSpan.FromMilliseconds(300);

	// Monaco loads asynchronously and a cold load can be slow; retry this many times before showing a failed state.
	private const int MaxInitAttempts = 9;

	/// <summary>Gets or sets the JavaScript runtime.</summary>
	[Inject]
	public IJSRuntime? JSRuntime { get; set; }

	/// <summary>Gets or sets the editor id.</summary>
	[Parameter]
	public string Id { get; set; } = $"PDDiffPanel-{++_seq}";

	/// <summary>Gets or sets the left-hand (before) text. A null value is treated as empty.</summary>
	[Parameter]
	public string Original { get; set; } = string.Empty;

	/// <summary>Gets or sets the right-hand (after) text. A null value is treated as empty.</summary>
	[Parameter]
	public string Modified { get; set; } = string.Empty;

	/// <summary>Gets or sets the file name shown in the toolbar and used to infer the language.</summary>
	[Parameter]
	public string? FileName { get; set; }

	/// <summary>Gets or sets an explicit Monaco language id, overriding the one inferred from <see cref="FileName"/>.</summary>
	[Parameter]
	public string? Language { get; set; }

	/// <summary>
	/// Gets or sets whether the two sides are shown side by side (true) or inline (false). Defaults to true.
	/// A toolbar button toggles the layout at runtime until the parent changes this value.
	/// </summary>
	[Parameter]
	public bool SideBySide { get; set; } = true;

	/// <summary>Gets or sets whether the modified side is read-only. Defaults to true.</summary>
	[Parameter]
	public bool ReadOnly { get; set; } = true;

	/// <summary>Gets or sets the CSS height of the panel. Defaults to 100%.</summary>
	[Parameter]
	public string Height { get; set; } = "100%";

	/// <summary>
	/// Gets or sets the Monaco theme; empty uses Monaco's default. Monaco themes are global to the page: panels with
	/// different themes end up with whichever was set last, and setting this back to empty does not revert it.
	/// </summary>
	[Parameter]
	public string Theme { get; set; } = string.Empty;

	// The language id in effect.
	internal string EffectiveLanguage => Language ?? MonacoLanguage.FromFileName(FileName);

	// Whether the two texts differ.
	internal bool HasChanges => !string.Equals(Original ?? string.Empty, Modified ?? string.Empty, StringComparison.Ordinal);

	private StandaloneDiffEditorConstructionOptions GetOptions(StandaloneDiffEditor _)
		=> new()
		{
			AutomaticLayout = true,
			ReadOnly = ReadOnly,
			OriginalEditable = false,
			RenderSideBySide = _sideBySide,
			UseInlineViewWhenSpaceIsLimited = false,
			Theme = string.IsNullOrEmpty(Theme) ? null : Theme
		};

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		// Only a change of the parent's value resets the layout; a user toggle survives re-renders.
		if (!_sideBySideSeen || SideBySide != _lastParamSideBySide)
		{
			_sideBySide = SideBySide;
			_lastParamSideBySide = SideBySide;
			_sideBySideSeen = true;
		}
	}

	private async Task OnEditorInitAsync()
	{
		if (_editor is null || JSRuntime is null)
		{
			return;
		}

		_editorCreated = true;

		// Capture first: the parameters can change while the awaits below are pending.
		var (original, modified, language, theme) = (Original ?? string.Empty, Modified ?? string.Empty, EffectiveLanguage, Theme);
		var (sideBySide, readOnly) = (_sideBySide, ReadOnly);
		TextModel? originalModel = null;
		TextModel? modifiedModel = null;
		try
		{
			originalModel = await Global.CreateModel(JSRuntime, original, language, null).ConfigureAwait(true);
			modifiedModel = await Global.CreateModel(JSRuntime, modified, language, null).ConfigureAwait(true);
			if (_disposed)
			{
				// Disposed while the models were being created: release the editor and do not leak the models.
				await ReleaseEditorAsync().ConfigureAwait(true);
				await DisposeModelsAsync(originalModel, modifiedModel).ConfigureAwait(true);
				return;
			}

			// Publish the models before attaching them so a concurrent dispose releases them in the right order.
			_originalModel = originalModel;
			_modifiedModel = modifiedModel;
			await _editor.SetModel(new DiffEditorModel { Original = originalModel, Modified = modifiedModel }).ConfigureAwait(true);
			if (_disposed)
			{
				await ReleaseEditorAsync().ConfigureAwait(true);
				return;
			}
		}
		catch (Exception ex) when (ex is JSException or JSDisconnectedException)
		{
			// Monaco is probably not loaded yet, or the circuit went away. Clean up and, unless disposed, retry
			// by recreating the editor component (the JS editor is only created on first render).
			_originalModel = null;
			_modifiedModel = null;
			await DisposeModelsAsync(originalModel, modifiedModel).ConfigureAwait(true);
			if (!_disposed && ex is JSException && ++_initAttempts <= MaxInitAttempts)
			{
				await Task.Delay(_initRetryDelay * Math.Min(1 << (_initAttempts - 1), 16)).ConfigureAwait(true);
				if (!_disposed)
				{
					_editorGeneration++;
					await InvokeAsync(StateHasChanged).ConfigureAwait(true);
				}
			}
			else if (!_disposed && ex is JSException)
			{
				// Out of retries: say so instead of leaving a blank panel.
				_loadFailed = true;
				await InvokeAsync(StateHasChanged).ConfigureAwait(true);
			}

			return;
		}

		(_shownOriginal, _shownModified, _shownLanguage, _shownTheme) = (original, modified, language, theme);
		(_shownSideBySide, _shownReadOnly) = (sideBySide, readOnly);
		_initialized = true;
		await SyncAsync().ConfigureAwait(true);
	}

	/// <inheritdoc />
	protected override Task OnParametersSetAsync() => SyncAsync();

	// Pushes any drift between the parameters and what the live editor shows.
	private async Task SyncAsync()
	{
		if (!_initialized || _editor is null || _originalModel is null || _modifiedModel is null || JSRuntime is null)
		{
			return;
		}

		try
		{
			var (original, modified) = (Original ?? string.Empty, Modified ?? string.Empty);
			if (_shownOriginal != original || _shownModified != modified)
			{
				// Model.setValue resets the scroll position, so swap the text in JavaScript and restore the scroll.
				var module = await GetModuleAsync().ConfigureAwait(true);
				await module.InvokeVoidAsync("setTexts", Id, _shownOriginal != original ? original : null, _shownModified != modified ? modified : null).ConfigureAwait(true);
				(_shownOriginal, _shownModified) = (original, modified);
			}

			if (_shownLanguage != EffectiveLanguage)
			{
				await Global.SetModelLanguage(JSRuntime, _originalModel, EffectiveLanguage).ConfigureAwait(true);
				await Global.SetModelLanguage(JSRuntime, _modifiedModel, EffectiveLanguage).ConfigureAwait(true);
				_shownLanguage = EffectiveLanguage;
			}

			// Same rule as PDMonacoEditor: only a non-empty, changed theme is pushed to the live editor.
			if (!string.IsNullOrEmpty(Theme) && Theme != _shownTheme)
			{
				await Global.SetTheme(JSRuntime, Theme).ConfigureAwait(true);
				_shownTheme = Theme;
			}

			if (_shownSideBySide != _sideBySide || _shownReadOnly != ReadOnly)
			{
				var (sideBySide, readOnly) = (_sideBySide, ReadOnly);
				await _editor.UpdateOptions(new DiffEditorOptions { RenderSideBySide = sideBySide, ReadOnly = readOnly, UseInlineViewWhenSpaceIsLimited = false }).ConfigureAwait(true);
				(_shownSideBySide, _shownReadOnly) = (sideBySide, readOnly);
			}
		}
		catch (Exception ex) when (ex is JSException or JSDisconnectedException)
		{
			// The editor can be disposed before the parameters are applied; safe to ignore.
		}
	}

	private async Task ToggleLayoutAsync()
	{
		_sideBySide = !_sideBySide;
		await SyncAsync().ConfigureAwait(true);
	}

	private static async Task DisposeModelsAsync(params TextModel?[] models)
	{
		foreach (var model in models)
		{
			if (model is null)
			{
				continue;
			}

			try
			{
				await model.DisposeModel().ConfigureAwait(true);
			}
			catch (Exception ex) when (ex is JSException or JSDisconnectedException)
			{
				// The JS runtime may already be gone; nothing left to release.
			}
		}
	}

	private async Task<IJSObjectReference> GetModuleAsync()
		=> _module ??= await JSRuntime!.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDDiffPanel.razor.js").ConfigureAwait(true);

	// Detaches the models from the diff widget and disposes the widget, then disposes the models.
	private async Task ReleaseEditorAsync()
	{
		var (original, modified) = (_originalModel, _modifiedModel);
		_originalModel = null;
		_modifiedModel = null;
		try
		{
			// Only once the JS editor exists (never during prerendering).
			if (JSRuntime is not null && _editorCreated)
			{
				_editorCreated = false;
				var module = await GetModuleAsync().ConfigureAwait(true);
				await module.InvokeVoidAsync("releaseEditor", Id).ConfigureAwait(true);
			}
		}
		catch (Exception ex) when (ex is JSException or JSDisconnectedException)
		{
			// Ignore failures during disposal; the editor or JS runtime may already be gone.
		}

		// The module handle is only needed while the editor exists.
		var handle = _module;
		_module = null;
		if (handle is not null)
		{
			try
			{
				await handle.DisposeAsync().ConfigureAwait(true);
			}
			catch (Exception ex) when (ex is JSException or JSDisconnectedException)
			{
				// The JS runtime may already be gone.
			}
		}

		await DisposeModelsAsync(original, modified).ConfigureAwait(true);
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		_disposed = true;
		await ReleaseEditorAsync().ConfigureAwait(true);
	}
}
