using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDMessage"/> renders a message's form and reports its outcome.
/// </summary>
public partial class PDMessageTests
{
	/// <summary>
	/// Verifies that a message carrying a form renders it, and that dismissing and submitting it are reported
	/// to the enclosing chat's form context.
	/// </summary>
	[Fact]
	public async Task Form_IsRendered_AndItsOutcomeReachesTheContext()
	{
		Guid? dismissed = null;
		ChatFormSubmission? submitted = null;
		var message = MessageWithForm();
		var context = new ChatFormContext
		{
			OnDismissed = id => { dismissed = id; return Task.CompletedTask; },
			OnSubmitted = s => { submitted = s; return Task.CompletedTask; }
		};
		var component = RenderMessage(message, p => p.AddCascadingValue(context));
		var submission = new ChatFormSubmission { FormId = message.Form!.Id, Answers = [] };

		await component.InvokeAsync(() => component.FindComponent<PDFormMessage>().Instance.OnSubmitted.InvokeAsync(submission));
		component.Find("button.pdchat-form-dismiss").Click();

		submitted.Should().BeSameAs(submission);
		dismissed.Should().Be(message.Form.Id);
	}

	/// <summary>
	/// Verifies that outside a chat the form still renders and its outcome goes nowhere without error.
	/// </summary>
	[Fact]
	public async Task Form_WithoutAContext_StillRenders()
	{
		var message = MessageWithForm();
		var component = RenderMessage(message, p => p.Add(x => x.UseFullWidthMessages, false));

		component.FindAll(".pdchat-form").Should().ContainSingle();
		await component.InvokeAsync(() => component.FindComponent<PDFormMessage>().Instance.OnSubmitted.InvokeAsync(
			new ChatFormSubmission { FormId = message.Form!.Id, Answers = [] }));
		component.Find("button.pdchat-form-dismiss").Click();

		component.FindAll(".pdchat-form").Should().ContainSingle();
	}

	private static ChatMessage MessageWithForm()
	{
		var message = Message(false, MessageType.Form, "A question for you");
		message.Form = new ChatForm
		{
			Id = Guid.NewGuid(),
			Questions =
			[
				new ChatFormQuestion { Id = "q1", Header = "Colour", Question = "Favourite colour?", Kind = ChatFormAnswerKind.Text }
			]
		};
		return message;
	}
}
