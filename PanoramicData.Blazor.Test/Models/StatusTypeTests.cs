using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="StatusType"/>.</summary>
public class StatusTypeTests
{
	/// <summary>Each built-in status carries its name, icon and colour class.</summary>
	[Fact]
	public void BuiltInStatuses_HaveExpectedStyling()
	{
		AssertStatus(StatusType.Red, "red", "fas fa-times-circle", "text-danger");
		AssertStatus(StatusType.Amber, "amber", "fas fa-exclamation-triangle", "pdsc-icon-amber");
		AssertStatus(StatusType.Green, "green", "fas fa-check-circle", "text-success");
		AssertStatus(StatusType.Gray, "gray", "fas fa-question-circle", "text-secondary");
	}

	/// <summary>The built-in statuses are singletons.</summary>
	[Fact]
	public void BuiltInStatuses_AreSingletons()
	{
		StatusType.Red.Should().BeSameAs(StatusType.Red);
		StatusType.Red.Should().NotBeSameAs(StatusType.Green);
	}

	/// <summary>A custom status carries the values it was created with, and describes itself by name.</summary>
	[Fact]
	public void Custom_CarriesValues()
	{
		var status = StatusType.Custom("blue", "fas fa-info", "text-info");

		AssertStatus(status, "blue", "fas fa-info", "text-info");
		status.ToString().Should().Be("blue");
	}

	/// <summary>A custom status must have a non-blank name, icon and colour.</summary>
	[Theory]
	[InlineData("", "icon", "colour")]
	[InlineData("name", " ", "colour")]
	[InlineData("name", "icon", "")]
	public void Custom_BlankArgument_Throws(string name, string icon, string colour)
	{
		var act = () => StatusType.Custom(name, icon, colour);

		act.Should().Throw<ArgumentException>();
	}

	/// <summary>A custom status rejects null arguments.</summary>
	[Fact]
	public void Custom_NullArgument_Throws()
	{
		var act = () => StatusType.Custom(null!, "icon", "colour");

		act.Should().Throw<ArgumentNullException>();
	}

	private static void AssertStatus(StatusType status, string name, string icon, string colour)
	{
		status.Name.Should().Be(name);
		status.DefaultIconClass.Should().Be(icon);
		status.DefaultColorClass.Should().Be(colour);
		status.ToString().Should().Be(name);
	}
}
