using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that a toast duration a timer cannot take, such as infinity to mean "until closed", shows the toast rather than
/// failing.
/// </summary>
public partial class PDChatTests
{
	/// <summary>Verifies that an infinite or very long display duration keeps the toast up.</summary>
	[Theory]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NaN)]
	[InlineData(1e10)]
	public async Task A_toast_duration_a_timer_cannot_take_keeps_the_toast_up(double seconds)
	{
		var service = Toasting();
		service.ToastAutoDismiss = true;
		service.ToastDisplayDurationSeconds = seconds;
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.Receive(Message("Stays")));

		component.WaitForAssertion(() => component.FindAll(".pdchat-toast").Should().ContainSingle(), Patience);
	}
}
