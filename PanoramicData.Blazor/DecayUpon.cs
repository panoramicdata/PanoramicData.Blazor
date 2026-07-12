
namespace PanoramicData.Blazor;

/// <summary>
/// Specifies when the decay timer for a <see cref="PDAudioPad"/> starts.
/// </summary>
public enum DecayUpon
{
	/// <summary>Decay begins when the pad is pressed.</summary>
	Press,
	/// <summary>Decay begins when the pad is released.</summary>
	Release
}
