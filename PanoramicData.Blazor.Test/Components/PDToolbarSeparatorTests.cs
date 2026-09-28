using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDToolbarSeparator"/>: its markup classes and its enable state methods.
/// </summary>
public class PDToolbarSeparatorTests : BunitContext
{
	/// <summary>A default separator is visible, not shifted and renders its bar.</summary>
	[Fact]
	public void Defaults_RenderAVisibleBar()
	{
		var cut = Render<PDToolbarSeparator>();

		cut.Find("div.pdtoolbaritem").ClassList.Should().NotContain("pd-hidden").And.NotContain("align-right");
		cut.FindAll("div.pdtoolbarseparator > div.pdts-bar").Should().ContainSingle();
	}

	/// <summary>Hidden, shifted and custom-class parameters are applied to the right elements.</summary>
	[Fact]
	public void Parameters_ApplyClasses()
	{
		var cut = Render<PDToolbarSeparator>(p => p
			.Add(x => x.IsVisible, false)
			.Add(x => x.ShiftRight, true)
			.Add(x => x.ItemCssClass, "item-x")
			.Add(x => x.CssClass, "sep-x"));

		cut.Find("div.pdtoolbaritem").ClassList.Should().Contain("pd-hidden").And.Contain("align-right").And.Contain("item-x");
		cut.Find("div.pdtoolbarseparator").ClassList.Should().Contain("sep-x");
	}

	/// <summary>Disable, Enable and SetEnabled update IsEnabled.</summary>
	[Fact]
	public async Task EnableMethods_UpdateIsEnabled()
	{
		var cut = Render<PDToolbarSeparator>(p => p.Add(x => x.Key, "sep").Add(x => x.ToolTip, "tip"));

		await cut.InvokeAsync(cut.Instance.Disable);
		cut.Instance.IsEnabled.Should().BeFalse();
		await cut.InvokeAsync(cut.Instance.Enable);
		cut.Instance.IsEnabled.Should().BeTrue();
		await cut.InvokeAsync(() => cut.Instance.SetEnabled(false));
		cut.Instance.IsEnabled.Should().BeFalse();
		cut.Instance.Key.Should().Be("sep");
		cut.Instance.ToolTip.Should().Be("tip");
	}
}
