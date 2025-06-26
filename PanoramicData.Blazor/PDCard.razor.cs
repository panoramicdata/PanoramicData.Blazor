namespace PanoramicData.Blazor
{
	public partial class PDCard<TCard> where TCard : ICard
	{

		/// <summary>
		/// Whether this card is currently selected by the user.
		/// </summary>
		private bool _isSelected => ParentCardDeck.Selection.Contains(Card);

		private bool _isDragging => ParentCardDeck.DragState.IsDragging && _isSelected;

		#region Parameters
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

		/// <summary>
		/// The parent card deck that this card belongs to.
		/// </summary>
		[Parameter]
		public required PDCardDeck<TCard> ParentCardDeck { get; set; }

		#endregion

		private Dictionary<string, object?> GetCardAttributes(TCard card)
		{
			var dict = new Dictionary<string, object?>
		{
			{ "class", GetCardClass(card) },
			{ "draggable", $"{DraggingEnabled}".ToLowerInvariant() }, // Make sure the value is lowercase,
			
			// Visual Updates
			{ "onmouseup", (MouseEventArgs e) =>ParentCardDeck.AddToSelectionAsync(e, card)},
			{ "ondragstart", (DragEventArgs e) => ParentCardDeck.OnDragStartAsync(e, card) },
			{ "ondragend", (DragEventArgs e) => ParentCardDeck.OnDragEndAsync(e, card) },

			// Position Updates
			{ "ondragover",(DragEventArgs e) =>  ParentCardDeck.NotifyDragPositionAsync(e, card)}
		};

			return dict;
		}

		private string GetCardClass(TCard card)
		{
			var cardClass = new StringBuilder();
			cardClass.Append("card ");

			if (Css is null)
			{
				cardClass.Append("card-default");
			}
			else
			{
				cardClass.Append(CultureInfo.InvariantCulture, $"{Css(card)}");
			}

			// Visual Updates of user actions
			if (_isSelected)
			{
				cardClass.Append(" selected");
			}

			if (_isDragging)
			{
				cardClass.Append(" dragging");
			}

			return cardClass.ToString();
		}
	}
}