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

	private string GetIconClass() => Node?.Status switch
	{
		RollUpStatus.Red => RedIconClass,
		RollUpStatus.Amber => AmberIconClass,
		RollUpStatus.Green => GreenIconClass,
		_ => GrayIconClass
	};

	private string GetColorClass() => Node?.Status switch
	{
		RollUpStatus.Red => "pdsr-icon-red",
		RollUpStatus.Amber => "pdsr-icon-amber",
		RollUpStatus.Green => "pdsr-icon-green",
		_ => "pdsr-icon-gray"
	};

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

				await _module.InvokeVoidAsync("init", _triggerId, nodeJson, iconMap);
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
			catch { }
		}

		GC.SuppressFinalize(this);
	}
}
