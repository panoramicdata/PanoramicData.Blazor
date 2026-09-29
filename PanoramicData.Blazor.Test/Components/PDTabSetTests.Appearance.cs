using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that a change to a tab's CSS class or icon shows in the tab strip from the render that made it,
/// even though the strip is drawn before the tab receives its new parameters (#190).
/// </summary>
public partial class PDTabSetTests
{
	/// <summary>A new CSS class and icon on a tab appear in the strip without any further render.</summary>
	[Fact]
	public void A_changed_css_class_and_icon_show_in_the_strip_at_once()
	{
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.ChildContent, Tabs(new Spec("One") { Css = "before" }, new Spec("Two"))));

		component.Render(parameters => parameters
			.Add(p => p.ChildContent, Tabs(new Spec("One") { Css = "after", Icon = "fas fa-star" }, new Spec("Two"))));

		var first = component.FindAll("button.pdtabset-tab")[0];
		first.ClassList.Should().Contain("after").And.NotContain("before");
		first.QuerySelector("i.pdtabset-tab-icon.fa-star").Should().NotBeNull();
	}
}
