namespace PanoramicData.Blazor;

/// <summary>
/// An input component for entering multiple short text values (tags), with optional autocomplete
/// suggestions, free-text entry, validation limits and keyboard support.
/// </summary>
public partial class PDTagInput : ComponentBase, IDisposable
{
	private static int _sequence;

	private readonly List<string> _values = [];
	private List<string> _filteredSuggestions = [];
	private string _text = string.Empty;
	private bool _showDropdown;
	private bool _isInvalid;
	private int _activeIndex = -1;
	private ElementReference _inputRef;
	private CancellationTokenSource? _blurToken;
	private List<string>? _suppliedValues;
	private List<string>? _emittedValues;

	/// <summary>
	/// Gets the unique identifier of this component instance.
	/// </summary>
	[Parameter] public string Id { get; set; } = $"pd-taginput-{++_sequence}";

	/// <summary>
	/// Gets or sets additional HTML attributes (e.g. style, data-*) applied to the root element.
	/// </summary>
	[Parameter(CaptureUnmatchedValues = true)]
	public Dictionary<string, object>? AdditionalAttributes { get; set; }

	/// <summary>
	/// Gets or sets additional CSS classes applied to the root element.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the current tag values. Supports two-way binding via @bind-Values.
	/// </summary>
	[Parameter] public List<string> Values { get; set; } = [];

	/// <summary>
	/// An event callback invoked whenever the tag values change. A new list instance is supplied.
	/// </summary>
	[Parameter] public EventCallback<List<string>> ValuesChanged { get; set; }

	/// <summary>
	/// Gets or sets the optional autocomplete suggestions offered while typing.
	/// Suggestions already present in Values are not offered again.
	/// </summary>
	[Parameter] public IEnumerable<string>? Suggestions { get; set; }

	/// <summary>
	/// Gets or sets whether tags outside the Suggestions list may be entered. Defaults to true.
	/// </summary>
	[Parameter] public bool AllowFreeText { get; set; } = true;

	/// <summary>
	/// Gets or sets whether pending typed text is committed as a tag when the input loses focus.
	/// Only applies when AllowFreeText is true. Defaults to true.
	/// </summary>
	[Parameter] public bool AddOnBlur { get; set; } = true;

	/// <summary>
	/// Gets or sets whether duplicate detection and suggestion matching are case sensitive.
	/// Defaults to false.
	/// </summary>
	[Parameter] public bool CaseSensitive { get; set; }

	/// <summary>
	/// Gets or sets the maximum permitted length of a single tag. Zero means unlimited.
	/// </summary>
	[Parameter] public int MaxTagLength { get; set; }

	/// <summary>
	/// Gets or sets the maximum number of tags. Zero means unlimited.
	/// </summary>
	[Parameter] public int MaxTags { get; set; }

	/// <summary>
	/// Gets or sets whether the component is enabled. When false the input is hidden and
	/// tags cannot be added or removed.
	/// </summary>
	[Parameter] public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the component is read-only. Tags are displayed but cannot be
	/// added or removed.
	/// </summary>
	[Parameter] public bool IsReadOnly { get; set; }

	/// <summary>
	/// Gets or sets the placeholder text shown in the input when no tags are present.
	/// </summary>
	[Parameter] public string Placeholder { get; set; } = "Add tag...";

	/// <summary>
	/// Gets or sets an optional template used to render each tag's content.
	/// Receives the tag text as context. The remove button is rendered separately.
	/// </summary>
	[Parameter] public RenderFragment<string>? TagTemplate { get; set; }

	/// <summary>
	/// An event callback invoked when a tag is added. Argument is the added tag.
	/// </summary>
	[Parameter] public EventCallback<string> TagAdded { get; set; }

	/// <summary>
	/// An event callback invoked when a tag is removed. Argument is the removed tag.
	/// </summary>
	[Parameter] public EventCallback<string> TagRemoved { get; set; }

	/// <summary>
	/// An event callback invoked when an attempted tag is rejected (duplicate, too long,
	/// max tags reached, or not in the suggestions list when free text is disabled).
	/// </summary>
	[Parameter] public EventCallback<TagRejectedEventArgs> TagRejected { get; set; }

	private StringComparison Comparison => CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		// Only resync internal state when the parent supplies a genuinely new list instance.
		// This keeps unbound usage working (the same instance is supplied every render) and
		// avoids resyncing when a bound parent simply echoes back the list we just emitted.
		if (ReferenceEquals(Values, _suppliedValues) || ReferenceEquals(Values, _emittedValues))
		{
			_suppliedValues = Values;
			return;
		}

		_suppliedValues = Values;
		_values.Clear();
		if (Values is not null)
		{
			_values.AddRange(Values);
		}
	}

	/// <summary>
	/// Moves focus to the tag input element.
	/// </summary>
	public async Task FocusAsync() => await _inputRef.FocusAsync().ConfigureAwait(true);

	private bool ContainsValue(string tag) => _values.Exists(v => string.Equals(v, tag, Comparison));

	private async Task EmitValuesAsync()
	{
		_emittedValues = [.. _values];
		await ValuesChanged.InvokeAsync(_emittedValues).ConfigureAwait(true);
	}

	private async Task<bool> TryAddTagAsync(string text)
	{
		var tag = text.Trim();
		if (tag.Length == 0)
		{
			_text = string.Empty;
			return false;
		}

		if (MaxTags > 0 && _values.Count >= MaxTags)
		{
			await RejectAsync(tag, TagRejectionReason.MaxTagsReached).ConfigureAwait(true);
			return false;
		}

		if (MaxTagLength > 0 && tag.Length > MaxTagLength)
		{
			await RejectAsync(tag, TagRejectionReason.TooLong).ConfigureAwait(true);
			return false;
		}

		// Canonicalise to the suggestion's casing when it matches one
		var match = Suggestions?.FirstOrDefault(s => string.Equals(s, tag, Comparison));
		if (match is not null)
		{
			tag = match;
		}
		else if (!AllowFreeText)
		{
			await RejectAsync(tag, TagRejectionReason.NotInSuggestions).ConfigureAwait(true);
			return false;
		}

		if (ContainsValue(tag))
		{
			await RejectAsync(tag, TagRejectionReason.Duplicate).ConfigureAwait(true);
			return false;
		}

		_values.Add(tag);
		_text = string.Empty;
		ApplyFilter();
		_showDropdown = _showDropdown && _filteredSuggestions.Count > 0;
		await EmitValuesAsync().ConfigureAwait(true);
		await TagAdded.InvokeAsync(tag).ConfigureAwait(true);
		return true;
	}

	private async Task RejectAsync(string tag, TagRejectionReason reason)
	{
		_isInvalid = true;
		await TagRejected.InvokeAsync(new TagRejectedEventArgs(tag, reason)).ConfigureAwait(true);
	}

	private async Task RemoveTagAsync(string tag)
	{
		if (!IsEnabled || IsReadOnly)
		{
			return;
		}

		var index = _values.FindIndex(v => string.Equals(v, tag, Comparison));
		if (index < 0)
		{
			return;
		}

		var removed = _values[index];
		_values.RemoveAt(index);
		ApplyFilter();
		await EmitValuesAsync().ConfigureAwait(true);
		await TagRemoved.InvokeAsync(removed).ConfigureAwait(true);
	}

	private void ApplyFilter()
	{
		var unused = (Suggestions ?? []).Where(s => !ContainsValue(s));
		var search = _text.Trim();
		_filteredSuggestions = search.Length == 0
			? [.. unused]
			: [.. unused.Where(s => s.Contains(search, Comparison))];
		_activeIndex = _filteredSuggestions.Count > 0 ? 0 : -1;
	}

	private async Task OnInputAsync(ChangeEventArgs e)
	{
		_isInvalid = false;
		_text = e.Value?.ToString() ?? string.Empty;

		// Commas commit pending text as tags (also handles pasted comma-separated lists)
		if (_text.Contains(','))
		{
			var parts = _text.Split(',');
			for (var i = 0; i < parts.Length - 1; i++)
			{
				await TryAddTagAsync(parts[i]).ConfigureAwait(true);
			}

			_text = parts[^1];
		}

		ApplyFilter();
		_showDropdown = _filteredSuggestions.Count > 0;
	}

	private async Task OnKeyDownAsync(KeyboardEventArgs e)
	{
		switch (e.Key)
		{
			case "Enter":
				if (_showDropdown && _activeIndex >= 0 && _activeIndex < _filteredSuggestions.Count)
				{
					await TryAddTagAsync(_filteredSuggestions[_activeIndex]).ConfigureAwait(true);
				}
				else if (_text.Trim().Length > 0)
				{
					await TryAddTagAsync(_text).ConfigureAwait(true);
				}

				break;

			case "Backspace":
				if (_text.Length == 0 && _values.Count > 0)
				{
					await RemoveTagAsync(_values[^1]).ConfigureAwait(true);
				}

				break;

			case "ArrowDown":
				if (!_showDropdown)
				{
					ApplyFilter();
					_showDropdown = _filteredSuggestions.Count > 0;
				}
				else if (_filteredSuggestions.Count > 0)
				{
					_activeIndex = (_activeIndex + 1) % _filteredSuggestions.Count;
				}

				break;

			case "ArrowUp":
				if (_showDropdown && _filteredSuggestions.Count > 0)
				{
					_activeIndex = (_activeIndex - 1 + _filteredSuggestions.Count) % _filteredSuggestions.Count;
				}

				break;

			case "Escape":
				_showDropdown = false;
				break;
		}
	}

	private void OnFocus(FocusEventArgs e)
	{
		_blurToken?.Cancel();
		ApplyFilter();
		_showDropdown = _filteredSuggestions.Count > 0;
	}

	private async Task OnBlurAsync(FocusEventArgs e)
	{
		_blurToken?.Cancel();
		_blurToken?.Dispose();
		_blurToken = new CancellationTokenSource();

		try
		{
			// Delay so a click on a suggestion can complete before the dropdown hides
			await Task.Delay(250, _blurToken.Token).ConfigureAwait(true);
			_showDropdown = false;
			if (AddOnBlur && AllowFreeText && _text.Trim().Length > 0)
			{
				await TryAddTagAsync(_text).ConfigureAwait(true);
			}

			await InvokeAsync(StateHasChanged).ConfigureAwait(true);
		}
		catch (TaskCanceledException)
		{
			// Refocused before the delay elapsed - keep the dropdown open
		}
	}

	private async Task OnSuggestionClickAsync(string suggestion)
	{
		await TryAddTagAsync(suggestion).ConfigureAwait(true);
		await FocusAsync().ConfigureAwait(true);
	}

	private async Task OnContainerClickAsync()
	{
		if (IsEnabled && !IsReadOnly)
		{
			await FocusAsync().ConfigureAwait(true);
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_blurToken?.Cancel();
		_blurToken?.Dispose();
		GC.SuppressFinalize(this);
	}
}
