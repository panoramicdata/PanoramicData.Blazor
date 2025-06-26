namespace PanoramicData.Blazor
{
	public partial class PDAnimation
	{
		[Parameter]
		public required RenderFragment Element { get; set; }

		/// <summary>
		/// Indicates whether this card is currently being animated to a new position.
		/// </summary>
		private bool _isAnimating;

		private CardPosition? _position;

		// Java script Interop
		[Inject] IJSRuntime JSRuntime { get; set; } = null!;

		private IJSObjectReference? _module;

		protected async override Task OnAfterRenderAsync(bool firstRender)
		{
			// Load the Javascript module
			if (firstRender)
			{
				_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDAnimation.razor.js").ConfigureAwait(true);

				await UpdateElementPositionAsync();
			}
		}

		/// <summary>
		/// Updates the position of the card by querying the JavaScript module.
		/// </summary>
		/// <returns></returns>
		internal async Task UpdateElementPositionAsync()
		{
			// Cannot update position if the module isn't loaded or if the card is currently animating
			if (_module is not null && !_isAnimating)
			{
				_position = await _module.InvokeAsync<CardPosition?>("getPosition", Id).ConfigureAwait(true);
			}
		}

		/// <summary>
		/// Animates the card's position to its new location using the JavaScript module.
		/// </summary>
		/// <returns></returns>
		internal async Task AnimateElementPosition()
		{
			var animationTime = 0.3d;
			if (_module is not null)
			{
				_isAnimating = true;
				await _module.InvokeVoidAsync("animate", Id, _position, animationTime);

				_isAnimating = false;
				// Ensure the position is updated after the animation completes
				await Task.Delay((int)(animationTime * 1000)); // Convert seconds to milliseconds

				// Update Position after animation
				await UpdateElementPositionAsync();
			}
		}
	}
}