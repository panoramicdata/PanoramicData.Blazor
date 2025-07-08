using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor;

public partial class PDAnimation : IDisposable
{
	private static int _sequence;

	#region Parameters

	[Parameter]
	public override string Id { get; set; } = $"pd-animation-{++_sequence}";

	[Parameter]
	public required RenderFragment Element { get; set; }

	/// <summary>
	/// The time in seconds for the animation to complete when the element is moved.
	/// </summary>
	[Parameter]
	public double AnimationTime { get; set; } = 0.3d;

	/// <summary>
	/// The type of transition to apply to the animation.
	/// </summary>
	[Parameter]
	public AnimationTransition Transition { get; set; } = AnimationTransition.EaseOut;

	#endregion

	private readonly List<ElementPosition> _positions = [];

	// Java script Interop
	[Inject] IJSRuntime JSRuntime { get; set; } = null!;

	private IJSObjectReference? _module;

	/// <summary>
	/// The last time this animation was triggered.
	/// </summary>
	private TimeSpan _lastAnimationTrigger = TimeSpan.Zero;
	private bool _disposedValue;

	protected async override Task OnAfterRenderAsync(bool firstRender)
	{
		// Load the Javascript module
		if (firstRender)
		{
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDAnimation.razor.js").ConfigureAwait(true);
		}
		await UpdatePositionAsync();

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
		// Don't animate if the last animation was triggered too recently
		if (_lastAnimationTrigger + TimeSpan.FromSeconds(AnimationTime / 3) > DateTime.Now.TimeOfDay)
		{
			return;
		}

		await UpdatePositionAsync();

		if (_positions.Count < 2)
		{
			return;
		}

		if (_module is not null)
		{
			_lastAnimationTrigger = DateTime.Now.TimeOfDay;
			await _module.InvokeVoidAsync("animate", Id, _positions[^2], _positions[^1], AnimationTime, GetAnimationStyle());

			_positions.Clear();
		}
	}

	public void ClearPositions()
	{

		_positions.Clear();
	}

	public async Task CancelAnimationAsync()
	{
		if (_module is not null)
		{
			await _module.InvokeVoidAsync("cancelAnimation", Id);
		}
	}

	private string GetAnimationStyle()
		=> Transition switch
		{
			AnimationTransition.Initial => "initial",
			AnimationTransition.Inherit => "inherit",
			AnimationTransition.Linear => "linear",
			AnimationTransition.Ease => "ease",
			AnimationTransition.EaseIn => "ease-in",
			AnimationTransition.EaseOut => "ease-out",
			AnimationTransition.EaseInOut => "ease-in-out",
			AnimationTransition.StepStart => "step-start",
			AnimationTransition.StepEnd => "step-end",
			_ => "ease-in-out"
		};

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				// Dispose managed state (managed objects)
				_positions.Clear();
				_module = null;
			}
			// Free unmanaged resources (unmanaged objects) and override finalizer
			// Set large fields to null
			Element = default!;
			_disposedValue = true;
		}
	}

	// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	// ~PDAnimation()
	// {
	//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
	//     Dispose(disposing: false);
	// }

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}