using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor;

/// <summary>
/// A configurable content widget that supports multiple content types including
/// HTML, URL content, clock, image, and custom RenderFragment.
/// </summary>
public partial class PDWidget : PDComponentBase, IAsyncDisposable
{
	private static int _idSequence;
	private string _sanitizedContent = string.Empty;
	private string _clockTime = string.Empty;
	private string _clockDate = string.Empty;
	private string _imageSource = string.Empty;
	private string _errorMessage = string.Empty;
	private bool _isLoading;
	private bool _isRefreshing;
	private bool _hasError;
	private Timer? _refreshTimer;
	private Timer? _clockTimer;

	/// <summary>
	/// Gets or sets the widget title displayed in the header.
	/// </summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>
	/// Gets or sets the content type of the widget.
	/// </summary>
	[Parameter]
	public PDWidgetType WidgetType { get; set; } = PDWidgetType.Custom;

	/// <summary>
	/// Gets or sets the HTML content or URL depending on the widget type.
	/// </summary>
	[Parameter]
	public string? Content { get; set; }

	/// <summary>
	/// Gets or sets binary content for image widgets.
	/// </summary>
	[Parameter]
	public byte[]? ContentBytes { get; set; }

	/// <summary>
	/// Gets or sets a JSON configuration string for the widget.
	/// </summary>
	[Parameter]
	public string? Configuration { get; set; }

	/// <summary>
	/// Gets or sets per-widget CSS classes.
	/// </summary>
	[Parameter]
	public string? Css { get; set; }

	/// <summary>
	/// Gets or sets the auto-refresh interval in seconds. 0 = disabled.
	/// </summary>
	[Parameter]
	public int RefreshIntervalSeconds { get; set; }

	/// <summary>
	/// Gets or sets whether editing controls are shown.
	/// </summary>
	[Parameter]
	public bool IsEditable { get; set; }

	/// <summary>
	/// Gets or sets whether the title bar is shown.
	/// </summary>
	[Parameter]
	public bool ShowTitle { get; set; } = true;

	/// <summary>
	/// Gets or sets the child content for Custom widget type.
	/// </summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Gets or sets the callback fired on content refresh.
	/// </summary>
	[Parameter]
	public EventCallback OnRefresh { get; set; }

	/// <summary>
	/// Gets or sets the callback fired when the configure button is clicked.
	/// </summary>
	[Parameter]
	public EventCallback OnConfigure { get; set; }

	/// <summary>
	/// Gets or sets a delegate for fetching URL content. The string parameter is the URL.
	/// </summary>
	[Parameter]
	public Func<string, Task<string>>? FetchContent { get; set; }

	/// <summary>
	/// Gets or sets the clock timezone. Defaults to local time.
	/// </summary>
	[Parameter]
	public TimeZoneInfo? ClockTimeZone { get; set; }

	/// <summary>
	/// Gets or sets the clock time format string.
	/// </summary>
	[Parameter]
	public string ClockTimeFormat { get; set; } = "HH:mm:ss";

	/// <summary>
	/// Gets or sets the clock date format string.
	/// </summary>
	[Parameter]
	public string ClockDateFormat { get; set; } = "dddd, dd MMMM yyyy";

	/// <summary>
	/// Gets or sets the MIME type for image content bytes.
	/// </summary>
	[Parameter]
	public string ImageMimeType { get; set; } = "image/png";

	protected override void OnInitialized()
	{
		base.OnInitialized();
		if (Id == $"pd-component-{Sequence}")
		{
			Id = $"pd-widget-{++_idSequence}";
		}
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await LoadContentAsync().ConfigureAwait(true);
			SetupTimers();
		}
	}

	protected override async Task OnParametersSetAsync()
	{
		if (WidgetType == PDWidgetType.Clock)
		{
			UpdateClock();
		}
		else
		{
			await LoadContentAsync().ConfigureAwait(true);
		}
	}

	private async Task LoadContentAsync()
	{
		_hasError = false;
		_errorMessage = string.Empty;

		switch (WidgetType)
		{
			case PDWidgetType.Html:
				_sanitizedContent = HtmlSanitizer.Sanitize(Content);
				break;

			case PDWidgetType.Url:
				if (!string.IsNullOrWhiteSpace(Content))
				{
					await FetchUrlContentAsync(Content).ConfigureAwait(true);
				}
				break;

			case PDWidgetType.Clock:
				UpdateClock();
				break;

			case PDWidgetType.Image:
				LoadImage();
				break;

			case PDWidgetType.Custom:
				break;
		}

		StateHasChanged();
	}

	private async Task FetchUrlContentAsync(string url)
	{
		if (FetchContent is null)
		{
			_hasError = true;
			_errorMessage = "No FetchContent delegate provided.";
			return;
		}

		_isLoading = !_isRefreshing;
		StateHasChanged();

		try
		{
			var content = await FetchContent(url).ConfigureAwait(true);
			_sanitizedContent = HtmlSanitizer.Sanitize(content);
			_hasError = false;
		}
		catch (Exception ex)
		{
			_hasError = true;
			_errorMessage = $"Failed to load content: {ex.Message}";
		}
		finally
		{
			_isLoading = false;
			_isRefreshing = false;
		}
	}

	private void LoadImage()
	{
		if (ContentBytes is { Length: > 0 })
		{
			_imageSource = $"data:{ImageMimeType};base64,{Convert.ToBase64String(ContentBytes)}";
		}
		else if (!string.IsNullOrWhiteSpace(Content))
		{
			_imageSource = Content;
		}
		else
		{
			_imageSource = string.Empty;
		}
	}

	private void UpdateClock()
	{
		var now = ClockTimeZone is not null
			? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ClockTimeZone)
			: DateTime.Now;

		_clockTime = now.ToString(ClockTimeFormat, CultureInfo.CurrentCulture);
		_clockDate = now.ToString(ClockDateFormat, CultureInfo.CurrentCulture);
	}

	private void SetupTimers()
	{
		if (WidgetType == PDWidgetType.Clock)
		{
			_clockTimer = new Timer(_ =>
			{
				UpdateClock();
				InvokeAsync(StateHasChanged);
			}, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
		}

		if (RefreshIntervalSeconds > 0 && WidgetType is PDWidgetType.Html or PDWidgetType.Url or PDWidgetType.Image)
		{
			_refreshTimer = new Timer(async _ =>
			{
				_isRefreshing = true;
				await InvokeAsync(async () =>
				{
					await LoadContentAsync().ConfigureAwait(true);
					if (OnRefresh.HasDelegate)
					{
						await OnRefresh.InvokeAsync().ConfigureAwait(true);
					}
				}).ConfigureAwait(false);
			}, null, TimeSpan.FromSeconds(RefreshIntervalSeconds), TimeSpan.FromSeconds(RefreshIntervalSeconds));
		}
	}

	/// <summary>
	/// Manually triggers a content refresh.
	/// </summary>
	public async Task RefreshAsync()
	{
		_isRefreshing = true;
		await LoadContentAsync().ConfigureAwait(true);
		if (OnRefresh.HasDelegate)
		{
			await OnRefresh.InvokeAsync().ConfigureAwait(true);
		}
	}

	public ValueTask DisposeAsync()
	{
		_refreshTimer?.Dispose();
		_clockTimer?.Dispose();
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}
}
