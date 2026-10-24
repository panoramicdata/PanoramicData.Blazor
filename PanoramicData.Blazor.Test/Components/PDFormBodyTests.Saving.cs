using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Saving tests for <see cref="PDFormBody{TItem}"/>: what its editors have sent to the form by the time Save is clicked.
/// </summary>
public partial class PDFormBodyTests
{
	/// <summary>Leaving a debounced text or password field sends what was typed at once, so a Save clicked straight after saves it.</summary>
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Save_straight_after_leaving_a_debounced_field_saves_what_was_typed(bool isPassword)
	{
		var host = RenderForm([Field(x => x.Name, extra: new() { [nameof(PDField<Person>.IsPassword)] = isPassword })], debounceWait: 1000);
		await Edit(host, FormModes.Edit);

		await host.Find("input").ChangeAsync(new ChangeEventArgs { Value = "Grace" });
		var saved = await host.InvokeAsync(() => FormOf(host).SaveAsync());

		saved.Should().BeTrue();
		_person.Name.Should().Be("Grace");
	}
}
