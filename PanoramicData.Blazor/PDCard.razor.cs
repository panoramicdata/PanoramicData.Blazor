namespace PanoramicData.Blazor
{
	public partial class PDCard<TCard> where TCard : ICard
	{
		/// <summary>
		/// Whether Dragging is enabled for this card.
		/// </summary>
		[Parameter]
		public bool DraggingEnabled { get; set; } = true;

		/// <summary>
		/// The Template to render for the card.
		/// </summary>
		[Parameter]
		public RenderFragment<TCard>? Template { get; set; }

		/// <summary>
		/// Function returning CSS class that will be applied to the card.
		/// </summary>
		[Parameter]
		public Func<TCard, string>? Css { get; set; }

		/// <summary>
		/// The card data that is associated with this component.
		/// </summary>
		[Parameter]
		public required TCard Card { get; set; }

		private Dictionary<string, object?> GetCardAttributes(TCard card)
		{
			var dict = new Dictionary<string, object?>
		{
			{ "class", GetCardClass(card) },
			{ "draggable", $"{DraggingEnabled}".ToLowerInvariant() } // Make sure the value is lowercase
		};

			return dict;
		}

		private string GetCardClass(TCard card)
		{
			if (Css is null)
			{
				return "card card-default";
			}

			return $"card {Css(card)}";
		}
	}
}