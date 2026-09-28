using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDConfirm"/>: the Yes / No / Cancel dialog and the outcome it hands back.
/// </summary>
public class PDConfirmTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDConfirmTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private static AngleSharp.Dom.IElement Button(IRenderedComponent<PDConfirm> confirm, string text)
		=> confirm.FindAll("button").Single(b => b.TextContent.Trim() == text);

	private static bool IsHidden(AngleSharp.Dom.IElement button)
		=> button.Closest(".pdtoolbaritem")!.ClassList.Contains("pd-hidden");

	/// <summary>
	/// Verifies the defaults: a titled dialog asking "Are you sure?", with Yes and No shown and Cancel hidden.
	/// </summary>
	[Fact]
	public void Defaults_AskAreYouSure_WithYesAndNoOnly()
	{
		var confirm = Render<PDConfirm>();

		confirm.Find(".modal-title").TextContent.Should().Be("Confirm Action");
		confirm.Find(".confirm-message").TextContent.Should().Be("Are you sure?");
		confirm.FindAll(".btn-close").Should().BeEmpty();
		IsHidden(Button(confirm, "Yes")).Should().BeFalse();
		IsHidden(Button(confirm, "No")).Should().BeFalse();
		IsHidden(Button(confirm, "Cancel")).Should().BeTrue();
	}

	/// <summary>
	/// Verifies that the button texts and the Cancel button's visibility follow the parameters.
	/// </summary>
	[Fact]
	public void ButtonText_AndCancelVisibility_FollowTheParameters()
	{
		var confirm = Render<PDConfirm>(parameters => parameters
			.Add(p => p.YesText, "Delete")
			.Add(p => p.NoText, "Keep")
			.Add(p => p.CancelText, "Back")
			.Add(p => p.ShowCancel, true)
			.Add(p => p.Message, "Delete the file?"));

		confirm.Find(".confirm-message").TextContent.Should().Be("Delete the file?");
		IsHidden(Button(confirm, "Delete")).Should().BeFalse();
		IsHidden(Button(confirm, "Keep")).Should().BeFalse();
		IsHidden(Button(confirm, "Back")).Should().BeFalse();
	}

	/// <summary>
	/// Verifies that child content replaces the message.
	/// </summary>
	[Fact]
	public void ChildContent_ReplacesTheMessage()
	{
		var confirm = Render<PDConfirm>(parameters => parameters.AddChildContent("<p class=\"custom\">Custom body</p>"));

		confirm.FindAll(".confirm-message").Should().BeEmpty();
		confirm.Find(".custom").TextContent.Should().Be("Custom body");
	}

	/// <summary>
	/// Verifies that each button the user can press maps to its outcome.
	/// </summary>
	[Theory]
	[InlineData("Yes", PDConfirm.Outcomes.Yes)]
	[InlineData("No", PDConfirm.Outcomes.No)]
	[InlineData("Cancel", PDConfirm.Outcomes.Cancel)]
	public async Task ShowAndWaitResult_ReturnsTheOutcomeOfTheButtonPressed(string button, PDConfirm.Outcomes expected)
	{
		var confirm = Render<PDConfirm>(parameters => parameters.Add(p => p.ShowCancel, true));
		Task<PDConfirm.Outcomes>? pending = null;

		await confirm.InvokeAsync(() => { pending = confirm.Instance.ShowAndWaitResultAsync(); });
		Button(confirm, button).Click();

		(await pending!).Should().Be(expected);
	}

	/// <summary>
	/// Verifies that showing with a message puts that message in the dialog before the user answers.
	/// </summary>
	[Fact]
	public async Task ShowAndWaitResult_WithAMessage_ShowsIt()
	{
		var confirm = Render<PDConfirm>();
		Task<PDConfirm.Outcomes>? pending = null;

		await confirm.InvokeAsync(() => { pending = confirm.Instance.ShowAndWaitResultAsync("Overwrite the file?"); });

		confirm.Find(".confirm-message").TextContent.Should().Be("Overwrite the file?");
		Button(confirm, "No").Click();
		(await pending!).Should().Be(PDConfirm.Outcomes.No);
	}

	/// <summary>
	/// Verifies that showing with a message and title puts both in the dialog.
	/// </summary>
	[Fact]
	public async Task ShowAndWaitResult_WithAMessageAndTitle_ShowsBoth()
	{
		var confirm = Render<PDConfirm>();
		Task<PDConfirm.Outcomes>? pending = null;

		await confirm.InvokeAsync(() => { pending = confirm.Instance.ShowAndWaitResultAsync("Overwrite the file?", "Overwrite"); });

		confirm.Find(".modal-title").TextContent.Should().Be("Overwrite");
		confirm.Find(".confirm-message").TextContent.Should().Be("Overwrite the file?");
		Button(confirm, "Yes").Click();
		(await pending!).Should().Be(PDConfirm.Outcomes.Yes);
	}

	/// <summary>
	/// Verifies that the token overloads return the pressed button's outcome while the token is live.
	/// </summary>
	[Fact]
	public async Task ShowAndWaitResult_WithALiveToken_ReturnsThePressedOutcome()
	{
		var confirm = Render<PDConfirm>();
		Task<PDConfirm.Outcomes>? first = null;
		Task<PDConfirm.Outcomes>? second = null;

		await confirm.InvokeAsync(() => { first = confirm.Instance.ShowAndWaitResultAsync(CancellationToken.None); });
		Button(confirm, "Yes").Click();
		(await first!).Should().Be(PDConfirm.Outcomes.Yes);

		await confirm.InvokeAsync(() => { second = confirm.Instance.ShowAndWaitResultAsync("Sure?", CancellationToken.None); });
		Button(confirm, "No").Click();
		(await second!).Should().Be(PDConfirm.Outcomes.No);
	}

	/// <summary>
	/// Verifies that cancelling the wait through its token is reported as Cancel, whichever overload was used.
	/// </summary>
	[Fact]
	public async Task ShowAndWaitResult_WhenTheTokenIsCancelled_IsCancel()
	{
		var confirm = Render<PDConfirm>();
		using var cancelled = new CancellationTokenSource();
		await cancelled.CancelAsync();
		var outcomes = new List<PDConfirm.Outcomes>();

		await confirm.InvokeAsync(async () =>
		{
			outcomes.Add(await confirm.Instance.ShowAndWaitResultAsync(cancelled.Token));
			outcomes.Add(await confirm.Instance.ShowAndWaitResultAsync("Sure?", cancelled.Token));
			outcomes.Add(await confirm.Instance.ShowAndWaitResultAsync("Sure?", "Title", cancelled.Token));
		});

		outcomes.Should().Equal(PDConfirm.Outcomes.Cancel, PDConfirm.Outcomes.Cancel, PDConfirm.Outcomes.Cancel);
	}
}
