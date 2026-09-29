using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDStudioResults"/> reflects the execution status in its styling and icon, shows a
/// placeholder or a sandboxed results frame, and raises <see cref="PDStudioResults.ContentChanged"/> when its
/// content is changed through its methods.
/// </summary>
public class PDStudioResultsTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDStudioResultsTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>
	/// Verifies that each recognised status maps to its CSS class and icon, matching case-insensitively.
	/// </summary>
	/// <param name="status">The execution status text.</param>
	/// <param name="expectedClass">The expected status class on the root element.</param>
	/// <param name="expectedIcon">The expected icon class.</param>
	[Theory]
	[InlineData("Execution Complete", "status-complete", "fa-check-circle")]
	[InlineData("Ready", "status-ready", "fa-circle")]
	[InlineData("Timed out after 30s", "status-timeout", "fa-clock")]
	[InlineData("Request timeout", "status-timeout", "fa-clock")]
	[InlineData("Cancelled by user", "status-cancelled", "fa-stop-circle")]
	[InlineData("Invalid code", "status-invalid", "fa-exclamation-triangle")]
	[InlineData("Runtime error", "status-runtime-error", "fa-bug")]
	[InlineData("Compilation ERROR", "status-error", "fa-times-circle")]
	[InlineData("Something else", "status-unknown", "fa-question-circle")]
	public void Status_MapsToClassAndIcon(string status, string expectedClass, string expectedIcon)
	{
		var component = Render<PDStudioResults>(parameters => parameters
			.Add(p => p.ExecutionStatus, status));

		component.Find("div.pd-studio-results").ClassList.Should().Contain(expectedClass);
		component.Find(".pd-studio-results-status i").ClassList.Should().Contain(expectedIcon);
		component.Find(".status-text").TextContent.Should().Be(status);
		component.FindAll(".spinner-border").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that while executing the status bar shows a spinner instead of an icon.
	/// </summary>
	[Fact]
	public void Executing_ShowsSpinner()
	{
		var component = Render<PDStudioResults>(parameters => parameters
			.Add(p => p.IsExecuting, true)
			.Add(p => p.ExecutionStatus, "Running"));

		component.Find("div.pd-studio-results").ClassList.Should().Contain("status-executing");
		component.Find(".spinner-border .visually-hidden").TextContent.Should().Be("Executing...");
		component.FindAll(".pd-studio-results-status i").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the status bar can be hidden, and that with no content the placeholder is shown.
	/// </summary>
	[Fact]
	public void NoStatusBar_AndNoContent_ShowsOnlyPlaceholder()
	{
		var component = Render<PDStudioResults>(parameters => parameters
			.Add(p => p.ShowStatusBar, false)
			.Add(p => p.CssClass, "mine")
			.Add(p => p.Content, "   "));

		component.Find("div.pd-studio-results").ClassList.Should().Contain("mine");
		component.FindAll(".pd-studio-results-status").Should().BeEmpty();
		component.Find(".pd-studio-results-placeholder").TextContent.Should().Contain("Execute code to see results here");
		component.FindAll("iframe").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that content is shown in a sandboxed frame, wrapped in an HTML document.
	/// </summary>
	[Fact]
	public void Content_IsShownInSandboxedFrame()
	{
		var component = Render<PDStudioResults>(parameters => parameters
			.Add(p => p.Content, "<p class=\"out\">42</p>"));

		var frame = component.Find("iframe.pd-studio-results-iframe");
		frame.GetAttribute("sandbox").Should().Be("allow-same-origin");
		var document = frame.GetAttribute("srcdoc")!;
		document.Should().Contain("<!DOCTYPE html>").And.Contain("<title>Results</title>");
		var body = document[(document.IndexOf("<body>", StringComparison.Ordinal) + "<body>".Length)..document.IndexOf("</body>", StringComparison.Ordinal)];
		body.Trim().Should().Be("<p class=\"out\">42</p>");
		component.FindAll(".pd-studio-results-placeholder").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that updating the results shows the new content and raises the change, and that a null update is
	/// treated as empty.
	/// </summary>
	[Fact]
	public async Task UpdateResults_ShowsContentAndRaisesChange()
	{
		var changes = new List<string>();
		var component = Render<PDStudioResults>(parameters => parameters
			.Add(p => p.ContentChanged, (string c) => changes.Add(c)));

		await component.InvokeAsync(() => component.Instance.UpdateResults("<b>done</b>"));
		component.Find("iframe").GetAttribute("srcdoc").Should().Contain("<b>done</b>");

		await component.InvokeAsync(() => component.Instance.UpdateResults(null!));
		component.FindAll("iframe").Should().BeEmpty();

		changes.Should().Equal("<b>done</b>", string.Empty);
	}

	/// <summary>
	/// Verifies that clearing the results restores the placeholder and raises the change with empty content.
	/// </summary>
	[Fact]
	public async Task ClearResults_RestoresPlaceholderAndRaisesChange()
	{
		var changes = new List<string>();
		var component = Render<PDStudioResults>(parameters => parameters
			.Add(p => p.Content, "<b>old</b>")
			.Add(p => p.ContentChanged, (string c) => changes.Add(c)));

		await component.InvokeAsync(component.Instance.ClearResults);

		component.Instance.Content.Should().BeEmpty();
		component.FindAll(".pd-studio-results-placeholder").Should().ContainSingle();
		changes.Should().Equal(string.Empty);
	}

	/// <summary>
	/// Verifies that the update and clear methods work with no change callback bound.
	/// </summary>
	[Fact]
	public async Task UpdateAndClear_WithoutCallback_StillUpdate()
	{
		var component = Render<PDStudioResults>();

		await component.InvokeAsync(() => component.Instance.UpdateResults("<i>x</i>"));
		component.FindAll("iframe").Should().ContainSingle();

		await component.InvokeAsync(component.Instance.ClearResults);
		component.FindAll("iframe").Should().BeEmpty();
	}
}
