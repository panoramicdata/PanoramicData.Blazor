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

	/// <summary>Gets or sets the JavaScript runtime.</summary>
	[Inject]
	public IJSRuntime? JSRuntime { get; set; }

	/// <summary>Gets or sets the editor id.</summary>
	[Parameter]
	public string Id { get; set; } = $"PDDiffPanel-{++_seq}";

	/// <summary>Gets or sets the left-hand (before) text.</summary>
	[Parameter]
	public string Original { get; set; } = string.Empty;

	/// <summary>Gets or sets the right-hand (after) text.</summary>
	[Parameter]
	public string Modified { get; set; } = string.Empty;

	/// <summary>Gets or sets the file name shown in the toolbar and used to infer the language.</summary>
	[Parameter]
	public string? FileName { get; set; }

	/// <summary>Gets or sets an explicit Monaco language id, overriding the one inferred from <see cref="FileName"/>.</summary>
	[Parameter]
	public string? Language { get; set; }

	/// <summary>Gets or sets whether the two sides are shown side by side (true) or inline (false).</summary>
	[Parameter]
	public bool SideBySide { get; set; } = true;

	/// <summary>Gets or sets whether the modified side is read-only.</summary>
	[Parameter]
	public bool ReadOnly { get; set; } = true;

	/// <summary>Gets or sets the CSS height of the panel.</summary>
	[Parameter]
	public string Height { get; set; } = "100%";

	/// <summary>Gets or sets the Monaco theme; empty uses Monaco's default.</summary>
	[Parameter]
	public string Theme { get; set; } = string.Empty;

	/// <summary>Gets the language id in effect.</summary>
	public string EffectiveLanguage => Language ?? DiffLanguage.FromFileName(FileName);

	/// <summary>Gets whether the two texts differ.</summary>
	public bool HasChanges => !string.Equals(Original, Modified, StringComparison.Ordinal);

	private StandaloneDiffEditorConstructionOptions GetOptions(StandaloneDiffEditor _)
		=> new()
		{
			AutomaticLayout = true,
			ReadOnly = ReadOnly,
			OriginalEditable = false,
			RenderSideBySide = SideBySide,
			Theme = string.IsNullOrEmpty(Theme) ? null : Theme
		};

	private async Task OnEditorInitAsync()
	{
		if (_editor is null || JSRuntime is null)
		{
			return;
		}

		_originalModel = await Global.CreateModel(JSRuntime, Original, EffectiveLanguage, null).ConfigureAwait(true);
		_modifiedModel = await Global.CreateModel(JSRuntime, Modified, EffectiveLanguage, null).ConfigureAwait(true);
		await _editor.SetModel(new DiffEditorModel { Original = _originalModel, Modified = _modifiedModel }).ConfigureAwait(true);
		(_shownOriginal, _shownModified, _shownLanguage, _shownTheme) = (Original, Modified, EffectiveLanguage, Theme);
		_initialized = true;
	}

	/// <inheritdoc />
	protected override async Task OnParametersSetAsync()
	{
		if (!_initialized || _editor is null || _originalModel is null || _modifiedModel is null || JSRuntime is null)
		{
			return;
		}

		try
		{
			if (_shownOriginal != Original)
			{
				await _originalModel.SetValue(Original).ConfigureAwait(true);
				_shownOriginal = Original;
			}

			if (_shownModified != Modified)
			{
				await _modifiedModel.SetValue(Modified).ConfigureAwait(true);
				_shownModified = Modified;
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
		}
		catch (JSException)
		{
			// The editor can be disposed before the parameters are applied; safe to ignore.
		}
	}

	private async Task ToggleLayoutAsync()
	{
		SideBySide = !SideBySide;
		if (_editor is not null)
		{
			await _editor.UpdateOptions(new DiffEditorOptions { RenderSideBySide = SideBySide }).ConfigureAwait(true);
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		try
		{
			if (_originalModel is not null)
			{
				await _originalModel.DisposeModel().ConfigureAwait(true);
			}

			if (_modifiedModel is not null)
			{
				await _modifiedModel.DisposeModel().ConfigureAwait(true);
			}
		}
		catch
		{
			// Ignore failures during disposal; the JS runtime may already be gone.
		}
		finally
		{
			_originalModel = null;
			_modifiedModel = null;
		}
	}
}
