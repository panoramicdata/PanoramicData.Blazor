using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that a form sent again with fewer questions shows a question that exists.
/// </summary>
public partial class PDFormMessageTests
{
	/// <summary>Verifies that showing the third question, then receiving a one-question form, shows that question.</summary>
	[Fact]
	public async Task A_form_sent_again_with_fewer_questions_shows_one_that_exists()
	{
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.Text), Q("q2", ChatFormAnswerKind.Text), Q("q3", ChatFormAnswerKind.Text));
		await component.FindAll(".pdchat-form-tab")[2].ClickAsync(new());

		component.Render(parameters => parameters
			.Add(p => p.Form, new ChatForm { Id = Guid.NewGuid(), Questions = [Q("q1", ChatFormAnswerKind.Text)] }));

		component.Find(".pdchat-form-question").TextContent.Should().Be("Question q1?");
	}
}
