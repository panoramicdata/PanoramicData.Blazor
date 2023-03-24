namespace PanoramicData.Blazor;

public partial class PDCardDeck<TItem>
{
	private static int _sequence;

	[Inject]
	public IJSRuntime JSRuntime { get; set; } = null!;

	[Parameter]
	public string Id { get; set; } = $"pd-toggleswitch-{++_sequence}";

	[EditorRequired]
	[Parameter]
	public RenderFragment<TItem>? CardTemplate { get; set; }

	[Parameter]
	public IEnumerable<TItem> Items { get; set; } = Array.Empty<TItem>();

	private string SizeCssClass => Size switch
	{
		ButtonSizes.Small => "sm",
		ButtonSizes.Large => "lg",
		_ => "md"
	};
}
