using System.Runtime.Serialization;

namespace PanoramicData.Blazor.Enums;

/// <summary>
/// Enumeration representing different types of animation transitions that can be applied to elements in a Blazor application.
/// </summary>
public enum AnimationTransition
{
	/// <summary>
	/// Linear transition with no easing.
	/// </summary>
	[EnumMember(Value = "linear")]
	Linear,

	/// <summary>
	/// Default easing function, that starts slow, speeds up, and then slows down again.
	/// </summary>
	[EnumMember(Value = "ease")]
	Ease,

	/// <summary>
	/// Starts slowly and accelerates towards the end.
	/// </summary>
	[EnumMember(Value = "ease-in")]
	EaseIn,

	/// <summary>
	/// Ends slowly after starting fast.
	/// </summary>
	[EnumMember(Value = "ease-out")]
	EaseOut,

	/// <summary>
	/// Both starts and ends slowly, with a faster middle section.
	/// </summary>
	[EnumMember(Value = "ease-in-out")]
	EaseInOut,

	/// <summary>
	/// Equivalent to steps(1, start), which creates a single step transition that starts at the beginning.
	/// </summary>
	[EnumMember(Value = "step-start")]
	StepStart,

	/// <summary>
	/// Equivalent to steps(1, end), which creates a single step transition that ends at the end.
	/// </summary>
	[EnumMember(Value = "step-end")]
	StepEnd,

	/// <summary>
	/// Sets the transition to its initial value, which is typically the default transition defined by the browser or user agent.
	/// </summary>
	[EnumMember(Value = "initial")]
	Initial,

	/// <summary>
	/// Inherits the transition value from its parent element, allowing for consistent transitions across nested elements.
	/// </summary>
	[EnumMember(Value = "inherit")]
	Inherit,
}
