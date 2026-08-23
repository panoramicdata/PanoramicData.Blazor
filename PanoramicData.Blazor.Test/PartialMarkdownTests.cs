using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests for the partial-markdown trimming used while a streamed answer is still arriving
/// (issue #98).
/// </summary>
/// <remarks>
/// The cases are the structures that actually mangle when cut in half, taken from watching a real
/// stream rather than from reading the markdown spec. The two that matter most are the unclosed code
/// fence, because it swallows the remainder of the reply, and the half-written table row, because a
/// table that grows a column per second is the most obviously broken thing a viewer can be shown.
/// </remarks>
public class PartialMarkdownTests
{
	/// <summary>Verifies that no input produces no content and no status note.</summary>
	[Fact]
	public void Empty_input_yields_empty_content()
	{
		var result = PartialMarkdown.Trim(null);

		result.Content.Should().BeEmpty();
		result.Writing.Should().BeNull();
	}

	/// <summary>Verifies that ordinary text passes through untouched.</summary>
	[Fact]
	public void Plain_prose_is_left_alone()
	{
		var result = PartialMarkdown.Trim("Here is what I found on the network");

		result.Content.Should().Be("Here is what I found on the network");
		result.Writing.Should().BeNull();
	}

	/// <summary>Verifies that an open code fence is removed, since it would otherwise swallow the rest of the reply.</summary>
	[Fact]
	public void An_unclosed_code_fence_is_trimmed_away()
	{
		// Left in, this swallows everything that follows it.
		var result = PartialMarkdown.Trim("Findings:\n\n```json\n{\"ssid\": \"Panoram");

		result.Content.Should().Be("Findings:");
		result.Writing.Should().Be("code block");
	}

	/// <summary>Verifies that a complete code block survives trimming.</summary>
	[Fact]
	public void A_closed_code_fence_is_kept()
	{
		var partial = "Findings:\n\n```json\n{}\n```";

		var result = PartialMarkdown.Trim(partial);

		result.Content.Should().Be(partial);
		result.Writing.Should().BeNull();
	}

	/// <summary>Verifies that a partially written table row is held back until it is complete.</summary>
	[Fact]
	public void A_half_written_table_row_is_trimmed_to_the_last_complete_row()
	{
		var partial = string.Join('\n',
			"| SSID | Auth |",
			"|---|---|",
			"| PanoramicData | WPA2 |",
			"| PanoramicData-Gu");

		var result = PartialMarkdown.Trim(partial);

		result.Content.Should().Be(string.Join('\n',
			"| SSID | Auth |",
			"|---|---|",
			"| PanoramicData | WPA2 |"));
		result.Writing.Should().Be("table");
	}

	/// <summary>Verifies that a table header alone is not rendered, since it is not yet a table.</summary>
	[Fact]
	public void A_table_with_no_delimiter_row_yet_is_held_back_entirely()
	{
		// A header alone is not a table, and renders as a stray row of pipes.
		var result = PartialMarkdown.Trim("Some text\n\n| SSID | Auth |");

		result.Content.Should().Be("Some text");
		result.Writing.Should().Be("table");
	}

	/// <summary>Verifies that a complete table survives trimming.</summary>
	[Fact]
	public void A_complete_table_is_kept()
	{
		var partial = string.Join('\n',
			"| SSID | Auth |",
			"|---|---|",
			"| PanoramicData | WPA2 |");

		var result = PartialMarkdown.Trim(partial);

		result.Content.Should().Be(partial);
		result.Writing.Should().BeNull();
	}

	/// <summary>Verifies that a heading with no text after it is held back.</summary>
	[Fact]
	public void A_dangling_heading_is_trimmed()
	{
		var result = PartialMarkdown.Trim("Findings so far\n\n## ");

		result.Content.Should().Be("Findings so far");
		result.Writing.Should().BeNull();
	}

	/// <summary>Verifies that a heading followed by content survives trimming.</summary>
	[Fact]
	public void A_heading_with_text_after_it_is_kept()
	{
		var partial = "## The open SSID\n\nIt has no authentication.";

		var result = PartialMarkdown.Trim(partial);

		result.Content.Should().Be(partial);
	}

	/// <summary>Verifies that a partial list item is kept, so growing text does not visibly jump backwards.</summary>
	[Fact]
	public void A_part_written_list_item_is_left_alone()
	{
		// Trimming this would make the text visibly jump backwards as it grows, which reads worse
		// than a word appearing a character at a time.
		var partial = "- The guest SSID is open\n- The staff SSID is W";

		var result = PartialMarkdown.Trim(partial);

		result.Content.Should().Be(partial);
		result.Writing.Should().BeNull();
	}

	/// <summary>Verifies that CRLF input is treated the same as LF input.</summary>
	[Fact]
	public void Windows_line_endings_are_handled()
	{
		var result = PartialMarkdown.Trim("Findings:\r\n\r\n```json\r\n{");

		result.Content.Should().Be("Findings:");
		result.Writing.Should().Be("code block");
	}
}
