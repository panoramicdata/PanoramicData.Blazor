using System.Text.Json;
using System.Text.Json.Serialization;

namespace PanoramicData.Blazor;

/// <summary>
/// Displays a status icon that, when clicked, opens a cascading pop-over showing
/// the full health hierarchy described by the Node parameter.
/// </summary>
public partial class PDStatusRollUp : IAsyncDisposable
{
	private static int _idSequence;
	private readonly string _triggerId = $"pdsr-{++_idSequence}";
	private IJSObjectReference? _module;
	private DotNetObjectReference<PDStatusRollUp>? _dotNetRef;

	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
	};

	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>Gets or sets the status tree root node.</summary>
	[Parameter] public PDStatusRollUpNode? Node { get; set; }

	/// <summary>Gets or sets an optional text label rendered beside the icon.</summary>
	[Parameter] public string? Label { get; set; }

	/// <summary>Gets or sets the CSS icon class used when Status is Red.</summary>
	[Parameter] public string RedIconClass { get; set; } = "fas fa-times-circle";

	/// <summary>Gets or sets the CSS icon class used when Status is Amber.</summary>
	[Parameter] public string AmberIconClass { get; set; } = "fas fa-exclamation-triangle";

	/// <summary>Gets or sets the CSS icon class used when Status is Green.</summary>
	[Parameter] public string GreenIconClass { get; set; } = "fas fa-check-circle";

	/// <summary>Gets or sets the CSS icon class used when Status is Gray (unknown).</summary>
	[Parameter] public string GrayIconClass { get; set; } = "fas fa-question-circle";

	/// <summary>
	/// Optional callback invoked just before a node's popup is shown (including drill-downs).
	/// Receives the node about to be expanded; return an updated node to replace it, or null to
	/// use the existing node unchanged. When not set the component behaves exactly as before.
	/// </summary>
	[Parameter] public Func<PDStatusRollUpNode, Task<PDStatusRollUpNode?>>? OnBeforeExpand { get; set; }

	private string GetIconClass() => Node?.Status switch
	{
		RollUpStatus.Red => RedIconClass,
		RollUpStatus.Amber => AmberIconClass,
		RollUpStatus.Green => GreenIconClass,
		_ => GrayIconClass
	};

	private string GetColorClass() => Node?.Status switch
	{
		RollUpStatus.Red => "text-danger",
		RollUpStatus.Amber => "pdsr-icon-amber",
		RollUpStatus.Green => "text-success",
		_ => "text-secondary"
	};

	/// <summary>
	/// Called from JavaScript when a node is about to be expanded.
	/// The nodePath is a dot-separated index path (e.g. "" = root, "0" = first child, "1.2" = second child's third child).
	/// Returns updated node JSON, or null if unchanged.
	/// </summary>
	[JSInvokable]
	public async Task<string?> ExpandNodeAsync(string nodePath)
	{
		if (OnBeforeExpand is null || Node is null)
		{
			return null;
		}

		var target = ResolveNode(Node, nodePath);
		if (target is null)
		{
			return null;
		}

		var updated = await OnBeforeExpand(target).ConfigureAwait(true);
		if (updated is null)
		{
			return null;
		}

		// Patch the live tree so future expansions see updated data.
		// For the root (empty path) we copy the updated properties onto the existing Node
		// instance and trigger a Blazor re-render so the trigger icon/label reflects the
		// new status without the consumer having to manage that themselves.
		if (string.IsNullOrEmpty(nodePath))
		{
			Node.Status = updated.Status;
			Node.Title = updated.Title;
			Node.Summary = updated.Summary;
			Node.Detail = updated.Detail;
			Node.Children = updated.Children;
			await InvokeAsync(StateHasChanged).ConfigureAwait(true);
		}
		else
		{
			PatchNode(Node, nodePath, updated);
		}

		return JsonSerializer.Serialize(updated, _jsonOptions);
	}

	private static PDStatusRollUpNode? ResolveNode(PDStatusRollUpNode root, string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return root;
		}

		var node = root;
		foreach (var segment in path.Split('.'))
		{
			if (!int.TryParse(segment, out var idx) || idx < 0 || idx >= node.Children.Count)
			{
				return null;
			}

			node = node.Children[idx];
		}

		return node;
	}

	private static void PatchNode(PDStatusRollUpNode root, string path, PDStatusRollUpNode replacement)
	{
		if (string.IsNullOrEmpty(path))
		{
			// Root replacement not supported - callers update root externally.
			return;
		}

		var segments = path.Split('.');
		var node = root;
		for (var i = 0; i < segments.Length - 1; i++)
		{
			if (!int.TryParse(segments[i], out var idx) || idx < 0 || idx >= node.Children.Count)
			{
				return;
			}

			node = node.Children[idx];
		}

		if (int.TryParse(segments[^1], out var lastIdx) && lastIdx >= 0 && lastIdx < node.Children.Count)
		{
			node.Children[lastIdx] = replacement;
		}
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && Node is not null)
		{
			try
			{
				_module = await JSRuntime.InvokeAsync<IJSObjectReference>(
					"import", "./_content/PanoramicData.Blazor/PDStatusRollUp.razor.js");

				var nodeJson = JsonSerializer.Serialize(Node, _jsonOptions);
				var iconMap = new
				{
					red = RedIconClass,
					amber = AmberIconClass,
					green = GreenIconClass,
					gray = GrayIconClass
				};

				DotNetObjectReference<PDStatusRollUp>? dotNetRef = null;
				if (OnBeforeExpand is not null)
				{
					_dotNetRef = DotNetObjectReference.Create(this);
					dotNetRef = _dotNetRef;
				}

				await _module.InvokeVoidAsync("init", _triggerId, nodeJson, iconMap, dotNetRef);
			}
			catch
			{
				// Fast page switching may dispose the module before init completes.
			}
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (_module is not null)
		{
			try
			{
				await _module.InvokeVoidAsync("dispose", _triggerId);
				await _module.DisposeAsync();
			}
			catch
			{
				// JS interop can fail during fast page navigation - safe to ignore.
			}
		}

		_dotNetRef?.Dispose();
		GC.SuppressFinalize(this);
	}
}
