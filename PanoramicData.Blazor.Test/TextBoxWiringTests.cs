using System.Text.RegularExpressions;
using AwesomeAssertions;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Guards how the library's text boxes send their text to the server (#285).
/// </summary>
/// <remarks>
/// A box wired as a one-way value with an oninput handler has its text written back by every render, which on a slow
/// connection lands after newer keystrokes and wipes them. Only @bind tells Blazor what the browser already shows.
/// </remarks>
public partial class TextBoxWiringTests
{
	private static readonly string[] _chatFiles = ["PDMessages.razor", "PDFormMessage.razor", "PDChatConversationSidebar.razor"];

	/// <summary>
	/// The chat's text boxes are bound, so typing on a slow connection is never written over.
	/// </summary>
	[Fact]
	public void TheChatsTextBoxes_AreBound_SoTypingIsNeverWrittenOver()
	{
		OneWayTextBoxes().Should().NotContain(box => _chatFiles.Contains(box.File));
	}

	/// <summary>
	/// Outside the chat, only the known text boxes are still wired one way.
	/// </summary>
	[Fact]
	public void TheOtherTextBoxesWiredOneWay_AreOnlyTheKnownOnes()
	{
		// #285 These are left for a follow-up, so a box added to this list is a new instance of the defect.
		OneWayTextBoxes()
			.Where(box => !_chatFiles.Contains(box.File))
			.GroupBy(box => box.File)
			.ToDictionary(group => group.Key, group => group.Count())
			.Should().BeEquivalentTo(new Dictionary<string, int>
			{
				["PDColorPicker.razor"] = 6,
				["PDDashboard.razor"] = 2,
				["PDDateTime.razor"] = 2,
				["PDDateTimeOffset.razor"] = 2,
				["PDMediaPlayer.razor"] = 2,
				["PDTable.razor"] = 1,
				["PDTabSet.razor"] = 1,
				["PDTagInput.razor"] = 1
			});
	}

	// Every input or textarea in the library whose value the server sets and whose oninput handler it also takes.
	private static List<(string File, int Line)> OneWayTextBoxes()
	{
		var boxes = new List<(string File, int Line)>();

		foreach (var path in Directory.EnumerateFiles(Path.Combine(PackagingGuardTests.RepositoryRoot, "PanoramicData.Blazor"), "*.razor", SearchOption.AllDirectories))
		{
			var text = File.ReadAllText(path);

			foreach (Match tag in TextBoxTagRegex().Matches(text))
			{
				if (ServerValueRegex().IsMatch(tag.Value) && OnInputRegex().IsMatch(tag.Value))
				{
					boxes.Add((Path.GetFileName(path), text[..tag.Index].Count(character => character == '\n') + 1));
				}
			}
		}

		return boxes;
	}

	// Quoted attribute values may hold ">", as in a lambda handler.
	[GeneratedRegex("""<(input|textarea)\b(?:[^>"]|"[^"]*")*>""", RegexOptions.Singleline)]
	private static partial Regex TextBoxTagRegex();

	[GeneratedRegex("""(?<![-\w])value\s*=\s*"?@""")]
	private static partial Regex ServerValueRegex();

	[GeneratedRegex(@"@oninput\b")]
	private static partial Regex OnInputRegex();
}
