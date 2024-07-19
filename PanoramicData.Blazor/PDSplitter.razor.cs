namespace PanoramicData.Blazor;

public partial class PDSplitter : IAsyncDisposable
{
	private static int _idSequence;
	private IJSObjectReference? _module;

	public string Id { get; private set; } = $"pdsplit-{++_idSequence}";

	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// Gets or sets the direction to split the contained panels.
	/// </summary>
	[Parameter] public SplitDirection Direction { get; set; }

	/// <summary>
	/// Gets or sets whether to expand panels to their min size, possibly overriding the default percentage size.
	/// </summary>
	[Parameter] public bool ExpandToMin { get; set; }

	/// <summary>
	/// Sets the gutter sizes in pixels.
	/// </summary>
	[Parameter] public int GutterSize { get; set; } = 10;

	/// <summary>
	/// Gets or sets the gutter alignment between elements.
	/// </summary>
	[Parameter] public string GutterAlign { get; set; } = "center";

	/// <summary>
	/// Sets the snap to minimum size offset in pixels.
	/// </summary>
	[Parameter] public int SnapOffset { get; set; } = 30;

	/// <summary>
	/// Sets the number of pixels to drag.
	/// </summary>
	[Parameter] public int DragInterval { get; set; } = 1;

	/// <summary>
	/// Child HTML content.
	/// </summary>
	[Parameter] public RenderFragment ChildContent { get; set; } = null!;

	/// <summary>
	/// Provides additional CSS classes for the containing element.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets a collection of child panels.
	/// </summary>
	private List<PDSplitPanel> Panels { get; } = new List<PDSplitPanel>();

	/// <summary>
	/// Adds the given panel to the list of available panels.
	/// </summary>
	/// <param name="panel">The PDSplitPanel to be added.</param>
	public void AddPanel(PDSplitPanel panel)
	{
		Panels.Add(panel);
		StateHasChanged();
	}

	protected async override Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			var ids = Panels.Select(x => $"#{x.Id}").ToArray();
			var sizesSum = Convert.ToDouble(Panels.Select(x => x.Size).Sum());
			var pcts = Panels.Select(x => (int)Math.Round((x.Size / sizesSum) * 100)).ToArray();
			var options = new SplitOptions
			{
				Direction = Direction.ToString().ToLowerInvariant(),
				MinSize = Panels.Select(x => x.MinSize).ToArray(),
				ExpandToMin = ExpandToMin,
				Sizes = pcts,
				GutterSize = GutterSize,
				SnapOffset = SnapOffset,
				DragInterval = DragInterval,
				Cursor = Direction == SplitDirection.Horizontal ? "col-resize" : "row-resize"
			};
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDSplitter.razor.js").ConfigureAwait(true);
			if (_module != null)
			{
				var available = await _module.InvokeAsync<bool>("hasSplitJs").ConfigureAwait(true);
				if (!available)
				{
					throw new PDSplitterException($"To use the {nameof(PDSplitter)} component you must include the split.js library");
				}

				await _module.InvokeVoidAsync("initialize", Id, ids, options).ConfigureAwait(true);
			}
		}
	}

	public async Task<double[]> GetSizesAsync()
	{
		if (_module != null)
		{
			return await _module.InvokeAsync<double[]>("getSizes", Id).ConfigureAwait(true);
		}

		return Array.Empty<double>();
	}

	public async Task SetSizesAsync(double[] sizes)
	{
		if (_module != null)
		{
			await _module.InvokeVoidAsync("setSizes", Id, sizes).ConfigureAwait(true);
		}
	}

	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_module != null)
			{
				await _module.InvokeVoidAsync("destroy", Id).ConfigureAwait(true);
				await _module.DisposeAsync().ConfigureAwait(true);
				_module = null;
			}
		}
		catch
		{
		}
	}
}
