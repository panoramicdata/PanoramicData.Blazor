namespace PanoramicData.Blazor
{
	public partial class PDAnimation
	{
		private static int _sequence;

		#region Parameters

		[Parameter]
		public override string Id { get; set; } = $"pd-animation-{++_sequence}";

		[Parameter]
		public required RenderFragment Element { get; set; }

		#endregion

		private List<ElementPosition> _positions = [];

		// Java script Interop
		[Inject] IJSRuntime JSRuntime { get; set; } = null!;

		private IJSObjectReference? _module;

		protected async override Task OnAfterRenderAsync(bool firstRender)
		{
			// Load the Javascript module
			if (firstRender)
			{
				_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDAnimation.razor.js").ConfigureAwait(true);
			}
		}

		/// <summary>
		/// Updates the position of the card by querying the JavaScript module.
		/// </summary>
		/// <returns></returns>
		public async Task UpdatePositionAsync()
		{
			if (_module is not null)
			{
				var newPosition = await _module.InvokeAsync<ElementPosition?>("getPosition", Id);

				// Could not find position
				if (newPosition is null)
				{
					return;
				}

				// Same position as the last moved one, no need to update
				if (_positions.Count != 0 && newPosition.Equals(_positions[^1]))
				{
					return;
				}

				_positions.Add(newPosition);

				if (_positions.Count > 2)
				{
					_positions.RemoveAt(0);
				}
			}
		}

		/// <summary>
		/// Animates the card's position to its new location using the JavaScript module.
		/// </summary>
		/// <returns></returns>
		public async Task AnimateElementAsync()
		{
			await UpdatePositionAsync();

			if (_positions.Count < 2)
			{
				return;
			}

			var animationTime = 0.3d;
			if (_module is not null)
			{
				await _module.InvokeVoidAsync("animate", Id, _positions[^2], _positions[^1], animationTime);

				_positions.RemoveAt(0);
			}
		}
	}
}