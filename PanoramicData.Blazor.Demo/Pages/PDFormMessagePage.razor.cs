using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;
using System.Globalization;
using System.Text;

namespace PanoramicData.Blazor.Demo.Pages;

/// <summary>
/// Demonstrates <see cref="PDFormMessage"/>, standalone and inside the chat (issue #106).
/// </summary>
public partial class PDFormMessagePage
{
	/// <summary>
	/// The same form the fake chat service produces, so the two demonstrations cannot drift apart.
	/// </summary>
	private readonly ChatForm _form = DumbChatService.BuildDemonstrationForm();

	private string? _outcome;

	private Task OnSubmittedAsync(ChatFormSubmission submission)
	{
		var builder = new StringBuilder();

		_ = builder.AppendLine(CultureInfo.InvariantCulture, $"Form {submission.FormId} submitted:");

		foreach (var answer in submission.Answers)
		{
			if (answer.WasSkipped)
			{
				_ = builder.AppendLine(CultureInfo.InvariantCulture, $"  {answer.QuestionId}: (skipped)");

				continue;
			}

			_ = builder.AppendLine(CultureInfo.InvariantCulture, $"  {answer.QuestionId}: {answer.Value}");

			if (answer.Values is not null)
			{
				_ = builder.AppendLine(
					CultureInfo.InvariantCulture,
					$"      selected: {string.Join(" | ", answer.Values)}");
			}

			if (answer.WasOther)
			{
				_ = builder.AppendLine(CultureInfo.InvariantCulture, $"      other: {answer.OtherText}");
			}

			if (answer.ScaleDescription is not null)
			{
				_ = builder.AppendLine(CultureInfo.InvariantCulture, $"      scale: {answer.ScaleDescription}");
			}
		}

		_outcome = builder.ToString();

		return Task.CompletedTask;
	}

	private Task OnDismissedAsync(Guid formId)
	{
		_outcome = string.Create(CultureInfo.InvariantCulture, $"Form {formId} was dismissed - nothing sent.");

		return Task.CompletedTask;
	}
}
