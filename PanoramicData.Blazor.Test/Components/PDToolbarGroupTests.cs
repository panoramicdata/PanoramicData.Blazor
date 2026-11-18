using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDToolbarGroup"/>: toolbar items joined into one group of buttons.
/// </summary>
public class PDToolbarGroupTests : BunitContext
{
	/// <summary>
	/// Verifies that the group is a toolbar item marked as a group, named for screen readers, holding its items.
	/// </summary>
	[Fact]
	public void Group_HoldsItsItems_AsANamedGroup()
	{
		var component = Render<PDToolbarGroup>(parameters => parameters
			.Add(p => p.Label, "Run")
			.Add(p => p.ItemCssClass, "my-group")
			.AddChildContent("<span class=\"inner\">Execute</span>"));

		var group = component.Find("div.pdtoolbaritem.pdtoolbargroup");
		group.GetAttribute("role").Should().Be("group");
		group.GetAttribute("aria-label").Should().Be("Run");
		group.ClassList.Should().Contain("my-group").And.NotContain("align-right").And.NotContain("pd-hidden");
		component.Find(".pdtoolbargroup > .inner").TextContent.Should().Be("Execute");
	}

	/// <summary>
	/// Verifies that a hidden group is hidden, and that shifting right adds the alignment class.
	/// </summary>
	[Fact]
	public void Hidden_AndShiftRight_SetTheirClasses()
	{
		Render<PDToolbarGroup>(parameters => parameters.Add(p => p.IsVisible, false))
			.Find("div.pdtoolbargroup").ClassList.Should().Contain("pd-hidden");

		Render<PDToolbarGroup>(parameters => parameters.Add(p => p.ShiftRight, true))
			.Find("div.pdtoolbargroup").ClassList.Should().Contain("align-right");
	}
}
