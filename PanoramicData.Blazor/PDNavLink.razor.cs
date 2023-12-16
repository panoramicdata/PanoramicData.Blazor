namespace PanoramicData.Blazor;

public partial class PDNavLink : IAsyncDisposable
{
	private const string _defaultActiveClass = "active";

	private bool _isActive;
	private string? _class;
	private string? _hrefAbsolute;
	private IJSObjectReference? _commonModule;

	[Inject]
	private IJSRuntime JSRuntime { get; set; } = default!;

	[Inject]
	private NavigationManager NavigationManager { get; set; } = default!;

	[Inject]
	private INavigationCancelService NavigationCancelService { get; set; } = default!;

	/// <summary>
	/// Gets or sets the CSS class name applied to the NavLink when the
	/// current route matches the NavLink href.
	/// </summary>
	[Parameter]
	public string? ActiveClass { get; set; }

	/// <summary>
	/// Gets or sets a collection of additional attributes that will be added to the generated
	/// <c>a</c> element.
	/// </summary>
	[Parameter(CaptureUnmatchedValues = true)]
	public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Gets or sets the computed CSS class based on whether or not the link is active.
	/// </summary>
	protected string? CssClass { get; set; }

	/// <summary>
	/// Gets or sets a value representing the URL matching behavior.
	/// </summary>
	[Parameter]
	public NavLinkMatch Match { get; set; }

	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			// To avoid leaking memory, it's important to detach any event handlers in Dispose()
			NavigationManager.LocationChanged -= OnLocationChanged;
			if (_commonModule != null)
			{
				await _commonModule.DisposeAsync().ConfigureAwait(true);
			}
		}
		catch
		{
		}
	}

	private async Task OnClick(MouseEventArgs args)
	{
		// if ctrl-key held down then open in new tab therefore no need to prompt
		if (args.CtrlKey && _commonModule != null)
		{
			// use JS to perform navigation in new tab
			await _commonModule.InvokeVoidAsync("openUrl", _hrefAbsolute, "_blank").ConfigureAwait(true);
		}
		else if (await NavigationCancelService.ProceedAsync(_hrefAbsolute ?? string.Empty).ConfigureAwait(true))
		{
			NavigationManager.NavigateTo(_hrefAbsolute ?? string.Empty);
		}
	}

	protected override async Task OnInitializedAsync()
	{
		// We'll consider re-rendering on each location change
		NavigationManager.LocationChanged += OnLocationChanged;
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime is not null)
		{
			_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/js/common.js");
		}
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		// Update computed state
		var href = (string?)null;
		if (AdditionalAttributes != null && AdditionalAttributes.TryGetValue("href", out var obj))
		{
			href = Convert.ToString(obj, CultureInfo.InvariantCulture);
		}

		_hrefAbsolute = href == null ? null : NavigationManager.ToAbsoluteUri(href).AbsoluteUri;
		_isActive = ShouldMatch(NavigationManager.Uri);

		_class = (string?)null;
		if (AdditionalAttributes != null && AdditionalAttributes.TryGetValue("class", out obj))
		{
			_class = Convert.ToString(obj, CultureInfo.InvariantCulture);
		}

		UpdateCssClass();
	}

	private void UpdateCssClass() => CssClass = _isActive ? CombineWithSpace(_class, ActiveClass ?? _defaultActiveClass) : _class;

	private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
	{
		// We could just re-render always, but for this component we know the
		// only relevant state change is to the _isActive property.
		var shouldBeActiveNow = ShouldMatch(args.Location);
		if (shouldBeActiveNow != _isActive)
		{
			_isActive = shouldBeActiveNow;
			UpdateCssClass();
			StateHasChanged();
		}
	}

	private bool ShouldMatch(string currentUriAbsolute)
	{
		if (_hrefAbsolute == null)
		{
			return false;
		}

		if (EqualsHrefExactlyOrIfTrailingSlashAdded(currentUriAbsolute))
		{
			return true;
		}

		if (Match == NavLinkMatch.Prefix
			&& IsStrictlyPrefixWithSeparator(currentUriAbsolute, _hrefAbsolute))
		{
			return true;
		}

		return false;
	}

	private bool EqualsHrefExactlyOrIfTrailingSlashAdded(string currentUriAbsolute)
	{
		System.Diagnostics.Debug.Assert(_hrefAbsolute != null);

		if (string.Equals(currentUriAbsolute, _hrefAbsolute, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (_hrefAbsolute?.Length > 0 && currentUriAbsolute.Length == _hrefAbsolute.Length - 1)
		{
			// Special case: highlight links to http://host/path/ even if you're
			// at http://host/path (with no trailing slash)
			//
			// This is because the router accepts an absolute URI value of "same
			// as base URI but without trailing slash" as equivalent to "base URI",
			// which in turn is because it's common for servers to return the same page
			// for http://host/vdir as they do for host://host/vdir/ as it's no
			// good to display a blank page in that case.
			if (_hrefAbsolute[^1] == '/'
				&& _hrefAbsolute.StartsWith(currentUriAbsolute, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static string? CombineWithSpace(string? str1, string str2)
		=> str1 == null ? str2 : $"{str1} {str2}";

	private static bool IsStrictlyPrefixWithSeparator(string value, string prefix)
	{
		var prefixLength = prefix.Length;
		if (value.Length > prefixLength)
		{
			return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
				&& (
					// Only match when there's a separator character either at the end of the
					// prefix or right after it.
					// Example: "/abc" is treated as a prefix of "/abc/def" but not "/abcdef"
					// Example: "/abc/" is treated as a prefix of "/abc/def" but not "/abcdef"
					prefixLength == 0
					|| !char.IsLetterOrDigit(prefix[prefixLength - 1])
					|| !char.IsLetterOrDigit(value[prefixLength])
				);
		}
		else
		{
			return false;
		}
	}
}
