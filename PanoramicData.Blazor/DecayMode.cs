
namespace PanoramicData.Blazor;

/// <summary>
/// Specifies how the value of a <see cref="PDAudioPad"/> decays after activation.
/// </summary>
public enum DecayMode
{
	/// <summary>The pad toggles between fully on and fully off with no gradual decay.</summary>
	Toggle,
	/// <summary>The pad value decays toward zero following an exponential curve based on <c>DecayHalfLife</c>.</summary>
	Exponential,
	/// <summary>The pad value decays toward zero in a linear fashion based on <c>DecayHalfLife</c>.</summary>
	Linear
}
