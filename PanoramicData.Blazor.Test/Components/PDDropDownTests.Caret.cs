using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// The caret: spaced from an icon or text beside it, and on its own with no gap and a text's height.
/// </summary>
public partial class PDDropDownTests
{
	/// <summary>Verifies that a caret beside text or an icon keeps its gap.</summary>
	[Theory]
	[InlineData("Options", "")]
	[InlineData("", "fas fa-cog")]
	public void A_caret_beside_text_or_an_icon_keeps_its_gap(string text, string icon)
	{
		var caret = Render<PDDropDown>(parameters => parameters.Add(p => p.Text, text).Add(p => p.IconCssClass, icon)).Find("button > i.fas");

		caret.ClassList.Should().Contain("ms-2").And.NotContain("pd-dropdown-caret-only");
	}

	/// <summary>Verifies that a caret on its own has no gap, and is marked to take a text's height.</summary>
	[Fact]
	public void A_caret_on_its_own_has_no_gap()
	{
		var caret = Render<PDDropDown>().Find("button > i.fas");

		caret.ClassList.Should().Contain("pd-dropdown-caret-only").And.Contain("fa-angle-down").And.NotContain("ms-2");
	}
}
