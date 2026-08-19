using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor;

/// <summary>
/// Displays a hierarchy as an area-proportional tree map, in the style of WizTree or WinDirStat,
/// where the area of each rectangle is proportional to a caller-supplied size metric.
/// </summary>
/// <typeparam name="TItem">The type of the item in the source hierarchy.</typeparam>
/// <remarks>
/// The layout is computed in C# by <see cref="TreeMapLayoutEngine"/> and rendered as inline SVG, so
/// the component takes no charting dependency. The only JavaScript involved reports the size of the
/// container, which the squarified algorithm needs in order to optimise rectangle aspect ratios.
/// Supplying <see cref="Width"/> and <see cref="Height"/> avoids that module entirely.
///
/// This component deliberately renders the tree map alone. Pair it with a PDTable bound to the same
/// data to reproduce the full WizTree experience.
/// </remarks>
public partial class PDTreeMap<TItem> : IAsyncDisposable where TItem : class
{
	private static int _idSequence;

	private readonly string _id = $"pdtm-{++_idSequence}";
	private readonly List<TItem> _breadcrumb = [];

	private IJSObjectReference? _module;
	private DotNetObjectReference<PDTreeMap<TItem>>? _dotNetRef;
	private ElementReference _canvasElement;
	private IReadOnlyList<TreeMapRect<TItem>> _rects = [];
	private TItem? _selected;
	private int _focusedIndex = -1;
	private double _width;
	private double _height;
	private bool _preventDefaultKey;
	private bool _heatRangeValid;
	private double _heatMinimum;
	private double _heatMaximum;

	/// <summary>
	/// Gets or sets the JavaScript runtime used to observe container size.
	/// </summary>
	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>Gets or sets the root of the hierarchy to display. The root itself is not drawn; its children fill the area.</summary>
	[Parameter] public TItem? Root { get; set; }

	/// <summary>Gets or sets the function returning the children of an item, or null for a leaf.</summary>
	[Parameter] public Func<TItem, IEnumerable<TItem>?>? ChildrenSelector { get; set; }

	/// <summary>Gets or sets the function returning the size of an item. This determines rectangle area and is required.</summary>
	[Parameter] public Func<TItem, double> SizeSelector { get; set; } = _ => 0;

	/// <summary>Gets or sets the function returning the label for an item.</summary>
	[Parameter] public Func<TItem, string>? TextSelector { get; set; }

	/// <summary>Gets or sets the function returning the hover text for an item.</summary>
	[Parameter] public Func<TItem, string>? TooltipSelector { get; set; }

	/// <summary>Gets or sets how a branch item's size is derived from its descendants.</summary>
	[Parameter] public TreeMapSizeMode SizeMode { get; set; } = TreeMapSizeMode.Aggregate;

	/// <summary>Gets or sets the number of levels drawn nested at once. Items at the cut still account for their whole subtree.</summary>
	[Parameter] public int MaxRenderDepth { get; set; } = 3;

	/// <summary>Gets or sets the item currently zoomed into. When null, the whole hierarchy is shown.</summary>
	[Parameter] public TItem? ZoomRoot { get; set; }

	/// <summary>Gets or sets the callback raised when the zoom target changes, supporting two-way binding.</summary>
	[Parameter] public EventCallback<TItem?> ZoomRootChanged { get; set; }

	/// <summary>Gets or sets a value indicating whether a breadcrumb showing the current path is displayed.</summary>
	[Parameter] public bool ShowBreadcrumb { get; set; } = true;

	/// <summary>Gets or sets how rectangle colours are determined.</summary>
	[Parameter] public TreeMapColourMode ColourMode { get; set; } = TreeMapColourMode.Category;

	/// <summary>Gets or sets the function returning an explicit CSS colour for an item. When supplied it overrides every other colour mode.</summary>
	[Parameter] public Func<TItem, string>? ColourSelector { get; set; }

	/// <summary>Gets or sets the function returning the grouping key used by the categorical palette.</summary>
	[Parameter] public Func<TItem, string>? CategorySelector { get; set; }

	/// <summary>Gets or sets the function returning the value used by the heat scale, independent of size.</summary>
	[Parameter] public Func<TItem, double>? HeatSelector { get; set; }

	/// <summary>Gets or sets the smallest rectangle dimension, in pixels, that will carry a label.</summary>
	[Parameter] public double MinLabelPx { get; set; } = 40;

	/// <summary>Gets or sets the label font size in pixels.</summary>
	[Parameter] public double LabelFontSize { get; set; } = 11;

	/// <summary>Gets or sets the inset between a rectangle edge and its label.</summary>
	[Parameter] public double LabelPadding { get; set; } = 4;

	/// <summary>Gets or sets the inset applied to a branch rectangle before its children are laid out.</summary>
	[Parameter] public double NestedPadding { get; set; } = 3;

	/// <summary>
	/// Gets or sets the space reserved at the top of a branch rectangle for its own label, so a parent
	/// label cannot collide with its children. Reserved only where the rectangle is tall enough.
	/// </summary>
	[Parameter] public double HeaderHeight { get; set; } = 18;

	/// <summary>Gets or sets the corner radius applied to each rectangle.</summary>
	[Parameter] public double CornerRadius { get; set; } = 2;

	/// <summary>Gets or sets an explicit width. When null, the width is measured from the container.</summary>
	[Parameter] public double? Width { get; set; }

	/// <summary>Gets or sets an explicit height. When null, the height is measured from the container.</summary>
	[Parameter] public double? Height { get; set; }

	/// <summary>Gets or sets the text shown when there is nothing to display.</summary>
	[Parameter] public string EmptyText { get; set; } = "No data to display";

	/// <summary>Gets or sets the accessible label applied to the tree map.</summary>
	[Parameter] public string AriaLabel { get; set; } = "Tree map";

	/// <summary>Gets or sets the callback raised when an item is clicked.</summary>
	[Parameter] public EventCallback<TItem> Click { get; set; }

	/// <summary>Gets or sets the callback raised when an item is double clicked.</summary>
	[Parameter] public EventCallback<TItem> DoubleClick { get; set; }

	/// <summary>Gets or sets the callback raised when the selected item changes.</summary>
	[Parameter] public EventCallback SelectionChanged { get; set; }

	/// <summary>Gets or sets the callback raised before the zoom target changes. Cancelling the arguments prevents the zoom.</summary>
	[Parameter] public EventCallback<TreeMapBeforeZoomEventArgs<TItem>> BeforeZoomChange { get; set; }

	/// <summary>Gets or sets the callback raised when an unexpected error occurs.</summary>
	[Parameter] public EventCallback<Exception> ExceptionHandler { get; set; }

	/// <summary>Gets the unique identifier of the outermost element.</summary>
	public string Id => _id;

	/// <summary>Gets the item currently selected, if any.</summary>
	public TItem? Selection => _selected;

	/// <summary>Gets the rectangles currently laid out, which is useful for tests and for co-ordinating a paired table.</summary>
	public IReadOnlyList<TreeMapRect<TItem>> Rectangles => _rects;

	private static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

	private string ViewBox => string.Create(CultureInfo.InvariantCulture, $"0 0 {_width:0.##} {_height:0.##}");

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		if (Width.HasValue)
		{
			_width = Width.Value;
		}

		if (Height.HasValue)
		{
			_height = Height.Value;
		}

		Rebuild();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Only observe the container when the caller has not fixed both dimensions.
		if (!firstRender || (Width.HasValue && Height.HasValue))
		{
			return;
		}

		try
		{
			_dotNetRef = DotNetObjectReference.Create(this);
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>(
				"import",
				"./_content/PanoramicData.Blazor/PDTreeMap.razor.js").ConfigureAwait(true);

			await _module.InvokeVoidAsync("init", _id, _canvasElement, _dotNetRef).ConfigureAwait(true);
		}
		catch (JSDisconnectedException)
		{
			// The circuit went away before the module loaded; nothing to clean up.
		}
		catch (ObjectDisposedException)
		{
			// Fast page switching can dispose the component before the import completes.
		}
		catch (Exception ex)
		{
			await InvokeExceptionHandlerAsync(ex).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Called from JavaScript when the container is resized.
	/// </summary>
	/// <param name="width">The new content width in pixels.</param>
	/// <param name="height">The new content height in pixels.</param>
	[JSInvokable]
	public void OnContainerResized(double width, double height)
	{
		var newWidth = Width ?? width;
		var newHeight = Height ?? height;

		// Ignore sub-pixel jitter, which would otherwise cause a re-render storm.
		if (Math.Abs(newWidth - _width) < 1 && Math.Abs(newHeight - _height) < 1)
		{
			return;
		}

		_width = newWidth;
		_height = newHeight;
		Rebuild();
		StateHasChanged();
	}

	private void Rebuild()
	{
		try
		{
			var layoutRoot = ZoomRoot ?? Root;

			_rects = TreeMapLayoutEngine.Layout(
				layoutRoot,
				ChildrenSelector,
				SizeSelector,
				_width,
				_height,
				Math.Max(1, MaxRenderDepth),
				NestedPadding,
				HeaderHeight,
				SizeMode);

			if (_focusedIndex >= _rects.Count)
			{
				_focusedIndex = _rects.Count - 1;
			}

			_heatRangeValid = false;
			RebuildBreadcrumb();
		}
		catch (Exception ex)
		{
			_rects = [];
			_ = InvokeExceptionHandlerAsync(ex);
		}
	}

	private void RebuildBreadcrumb()
	{
		_breadcrumb.Clear();

		if (Root is null)
		{
			return;
		}

		var target = ZoomRoot ?? Root;
		var path = new List<TItem>();

		if (FindPath(Root, target, path, []))
		{
			_breadcrumb.AddRange(path);
		}
		else
		{
			_breadcrumb.Add(Root);
		}
	}

	private bool FindPath(TItem current, TItem target, List<TItem> path, HashSet<TItem> visited)
	{
		if (!visited.Add(current))
		{
			return false;
		}

		path.Add(current);

		if (ReferenceEquals(current, target))
		{
			return true;
		}

		var children = ChildrenSelector?.Invoke(current);
		if (children is not null)
		{
			foreach (var child in children)
			{
				if (child is not null && FindPath(child, target, path, visited))
				{
					return true;
				}
			}
		}

		path.RemoveAt(path.Count - 1);
		return false;
	}

	private async Task OnNodeClickAsync(int index)
	{
		if (index < 0 || index >= _rects.Count)
		{
			return;
		}

		var rect = _rects[index];
		_focusedIndex = index;
		_selected = rect.Item;

		await SelectionChanged.InvokeAsync().ConfigureAwait(true);
		await Click.InvokeAsync(rect.Item).ConfigureAwait(true);
	}

	private async Task OnNodeDoubleClickAsync(TItem item)
	{
		await DoubleClick.InvokeAsync(item).ConfigureAwait(true);
		await ZoomToAsync(item).ConfigureAwait(true);
	}

	/// <summary>
	/// Zooms the tree map so that the given item fills the available area.
	/// </summary>
	/// <param name="item">The item to zoom to, or null to return to the root.</param>
	/// <returns>A task that completes when the zoom has been applied or cancelled.</returns>
	public async Task ZoomToAsync(TItem? item)
	{
		var target = ReferenceEquals(item, Root) ? null : item;

		if (ReferenceEquals(target, ZoomRoot))
		{
			return;
		}

		// A leaf has nothing to zoom into, so treat it as a selection instead.
		if (target is not null)
		{
			var children = ChildrenSelector?.Invoke(target);
			if (children is null || !children.Any())
			{
				return;
			}
		}

		if (BeforeZoomChange.HasDelegate)
		{
			var args = new TreeMapBeforeZoomEventArgs<TItem>(ZoomRoot, target);
			await BeforeZoomChange.InvokeAsync(args).ConfigureAwait(true);

			if (args.Cancel)
			{
				return;
			}
		}

		ZoomRoot = target;
		_focusedIndex = -1;
		Rebuild();

		await ZoomRootChanged.InvokeAsync(target).ConfigureAwait(true);
		StateHasChanged();
	}

	private async Task OnKeyDownAsync(KeyboardEventArgs args)
	{
		_preventDefaultKey = args.Key is "ArrowLeft" or "ArrowRight" or "ArrowUp" or "ArrowDown"
			or "Home" or "End" or "Enter" or " " or "Backspace";

		if (_rects.Count == 0)
		{
			return;
		}

		switch (args.Key)
		{
			case "ArrowRight":
			case "ArrowDown":
				_focusedIndex = Math.Min(_rects.Count - 1, _focusedIndex + 1);
				break;

			case "ArrowLeft":
			case "ArrowUp":
				_focusedIndex = Math.Max(0, _focusedIndex - 1);
				break;

			case "Home":
				_focusedIndex = 0;
				break;

			case "End":
				_focusedIndex = _rects.Count - 1;
				break;

			case "Enter":
			case " ":
				if (_focusedIndex >= 0)
				{
					await OnNodeClickAsync(_focusedIndex).ConfigureAwait(true);
					await ZoomToAsync(_rects[_focusedIndex].Item).ConfigureAwait(true);
				}

				return;

			case "Backspace":
			case "Escape":
				if (_breadcrumb.Count > 1)
				{
					await ZoomToAsync(_breadcrumb[^2]).ConfigureAwait(true);
				}

				return;

			default:
				return;
		}

		if (_focusedIndex >= 0)
		{
			_selected = _rects[_focusedIndex].Item;
			await SelectionChanged.InvokeAsync().ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Determines the fill colour of a rectangle. An explicit ColourSelector always wins, so a
	/// consumer can override any built-in scheme without abandoning the others.
	/// </summary>
	private string GetColour(TreeMapRect<TItem> rect)
	{
		try
		{
			if (ColourSelector is not null)
			{
				var explicitColour = ColourSelector(rect.Item);
				if (!string.IsNullOrWhiteSpace(explicitColour))
				{
					return explicitColour;
				}
			}

			return ColourMode switch
			{
				TreeMapColourMode.Category => TreeMapPalette.ForCategory(CategorySelector?.Invoke(rect.Item)),
				TreeMapColourMode.Depth => TreeMapPalette.ForDepth(rect.Depth, Math.Max(1, MaxRenderDepth) - 1),
				TreeMapColourMode.Heat => GetHeatColour(rect),
				_ => TreeMapPalette.Fallback()
			};
		}
		catch (Exception ex)
		{
			_ = InvokeExceptionHandlerAsync(ex);
			return TreeMapPalette.Fallback();
		}
	}

	private string GetHeatColour(TreeMapRect<TItem> rect)
	{
		if (HeatSelector is null)
		{
			return TreeMapPalette.Fallback();
		}

		EnsureHeatRange();

		return TreeMapPalette.ForHeat(HeatSelector(rect.Item), _heatMinimum, _heatMaximum);
	}

	/// <summary>
	/// Establishes the heat scale bounds across everything currently laid out, so that the scale is
	/// stable for one render rather than being recomputed per rectangle.
	/// </summary>
	private void EnsureHeatRange()
	{
		if (_heatRangeValid || HeatSelector is null)
		{
			return;
		}

		var minimum = double.MaxValue;
		var maximum = double.MinValue;

		foreach (var rect in _rects)
		{
			var value = HeatSelector(rect.Item);

			if (double.IsNaN(value) || double.IsInfinity(value))
			{
				continue;
			}

			minimum = Math.Min(minimum, value);
			maximum = Math.Max(maximum, value);
		}

		_heatMinimum = minimum == double.MaxValue ? 0 : minimum;
		_heatMaximum = maximum == double.MinValue ? 0 : maximum;
		_heatRangeValid = true;
	}

	private bool ShowLabel(TreeMapRect<TItem> rect)
	{
		if (rect.Width < MinLabelPx)
		{
			return false;
		}

		var required = LabelFontSize + (LabelPadding * 2);

		return rect.HasChildren && !rect.IsAggregated
			? rect.Height >= required
			: rect.Height >= Math.Min(MinLabelPx, required);
	}

	private string GetText(TItem item)
	{
		try
		{
			return TextSelector?.Invoke(item) ?? item.ToString() ?? string.Empty;
		}
		catch (Exception ex)
		{
			_ = InvokeExceptionHandlerAsync(ex);
			return string.Empty;
		}
	}

	private string GetTooltip(TreeMapRect<TItem> rect)
		=> TooltipSelector?.Invoke(rect.Item) ?? GetAccessibleName(rect);

	private string GetAccessibleName(TreeMapRect<TItem> rect)
	{
		var text = GetText(rect.Item);
		var size = rect.Size.ToString("N0", CultureInfo.CurrentCulture);

		return rect.IsAggregated
			? $"{text}, {size}, contains further items"
			: $"{text}, {size}";
	}

	/// <summary>
	/// Builds the absolute position of an HTML label overlaying its rectangle. Percentages are used
	/// so the labels track the SVG, which scales to the container.
	/// </summary>
	private string GetLabelStyle(TreeMapRect<TItem> rect)
	{
		if (_width <= 0 || _height <= 0)
		{
			return "display:none";
		}

		// A branch whose children are drawn keeps its label inside the header band; anything else may
		// use its whole rectangle.
		var boxHeight = rect.HasChildren && !rect.IsAggregated
			? Math.Min(rect.Height, HeaderHeight)
			: rect.Height;

		var left = rect.X / _width * 100;
		var top = rect.Y / _height * 100;
		var width = rect.Width / _width * 100;
		var height = boxHeight / _height * 100;

		return string.Create(
			CultureInfo.InvariantCulture,
			$"left:{left:0.###}%;top:{top:0.###}%;width:{width:0.###}%;height:{height:0.###}%;padding:{LabelPadding:0.#}px;font-size:{LabelFontSize:0.#}px");
	}

	private async Task InvokeExceptionHandlerAsync(Exception exception)
	{
		if (ExceptionHandler.HasDelegate)
		{
			await ExceptionHandler.InvokeAsync(exception).ConfigureAwait(true);
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_module is not null)
		{
			try
			{
				await _module.InvokeVoidAsync("dispose", _id).ConfigureAwait(true);
				await _module.DisposeAsync().ConfigureAwait(true);
			}
			catch (JSDisconnectedException)
			{
				// The circuit has already gone; the observer went with it.
			}
			catch (ObjectDisposedException)
			{
				// Already torn down.
			}

			_module = null;
		}

		_dotNetRef?.Dispose();
		_dotNetRef = null;

		GC.SuppressFinalize(this);
	}
}
