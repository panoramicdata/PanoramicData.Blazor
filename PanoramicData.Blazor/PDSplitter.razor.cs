namespace PanoramicData.Blazor;

public partial class PDSplitter : IDisposable
{
	private static int _idSequence;

	public string Id { get; private set; } = $"pdsplit-{++_idSequence}";

	[Inject] public IJSRuntime? JSRuntime { get; set; }

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

	protected async override Task OnInitializedAsync()
	{
		if (JSRuntime != null)
		{
			var available = await JSRuntime.InvokeAsync<bool>("panoramicData.hasSplitJs").ConfigureAwait(true);
			if (!available)
			{
				throw new PDSplitterException($"To use the {nameof(PDSplitter)} component you must include the split.js library");
			}
		}
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
				Direction = Direction.ToString().ToLower(),
				MinSize = Panels.Select(x => x.MinSize).ToArray(),
				ExpandToMin = ExpandToMin,
				Sizes = pcts,
				GutterSize = GutterSize,
				SnapOffset = SnapOffset,
				DragInterval = DragInterval,
				Cursor = Direction == SplitDirection.Horizontal ? "col-resize" : "row-resize"
			};
			if (JSRuntime != null)
			{
				await JSRuntime.InvokeVoidAsync("panoramicData.initializeSplitter", Id, ids, options).ConfigureAwait(true);
			}
		}
	}

	public async Task<double[]> GetSizesAsync()
	{
		if (JSRuntime != null)
		{
			return await JSRuntime.InvokeAsync<double[]>("panoramicData.splitterGetSizes", Id).ConfigureAwait(true);
		}
		return Array.Empty<double>();
	}

	public async Task SetSizesAsync(double[] sizes)
	{
		if (JSRuntime != null)
		{
			await JSRuntime.InvokeVoidAsync("panoramicData.splitterSetSizes", Id, sizes).ConfigureAwait(true);
		}
	}

	public void Dispose()
	{
		if (JSRuntime != null)
		{
			JSRuntime.InvokeVoidAsync("panoramicData.destroySplitter", Id);
		}
	}
}
