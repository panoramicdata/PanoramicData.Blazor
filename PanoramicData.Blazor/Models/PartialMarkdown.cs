using Markdig;
using System.Text.RegularExpressions;

namespace PanoramicData.Blazor.Models;

/// <summary>
/// Makes a partially-streamed markdown fragment safe to render, and says what is still arriving
/// (issue #98).
/// </summary>
/// <remarks>
/// <para>
/// A streamed answer is observed mid-structure far more often than not: half a table row, a heading
/// with no text after it, an opened code fence with no closing one. Rendering that raw produces
/// visible mangling - a one-column table that grows a column a second, a code block that swallows
/// the rest of the reply - and the mangling is what a viewer remembers.
/// </para>
/// <para>
/// The approach here is to trim back to the last point the fragment was structurally complete, and
/// to name what was trimmed. "… writing table" is a better thing to show than a broken table, and
/// it is also more honest: something *is* being written.
/// </para>
/// <para>
/// Deliberately conservative and deliberately not a markdown parser. It recognises the few
/// structures that actually break when cut in half, and leaves everything else alone. A fragment it
/// does not understand is returned unchanged, which is the same outcome as not having called it.
/// </para>
/// </remarks>
public static partial class PartialMarkdown
{
	/// <summary>
	/// The result of trimming: content safe to render, plus an optional note about what is still
	/// being written.
	/// </summary>
	/// <param name="Content">The fragment, trimmed to the last structurally complete point.</param>
	/// <param name="Writing">
	/// What was trimmed away, as a noun a reader would recognise - "table", "code block", "list" -
	/// or null when nothing needed trimming.
	/// </param>
	public sealed record Result(string Content, string? Writing);

	/// <summary>
	/// The pipeline used for streamed fragments. Built once - constructing one per keystroke of a
	/// streamed answer would be wasteful, and it holds no per-call state.
	/// </summary>
	private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
		.UseAdvancedExtensions()
		.Build();

	/// <summary>
	/// Renders a trimmed fragment as HTML.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="Trim"/> makes a fragment structurally safe; this is what makes it <i>look</i> like
	/// the finished answer. Without it a streamed reply shows its own markup - literal asterisks
	/// around bold text, pipe characters where a table will be - and then snaps into a rendered
	/// answer when the stream ends. The trimmer was always meant to feed a renderer; that half was
	/// missing.
	/// </para>
	/// <para>
	/// Only ever called on the output of <see cref="Trim"/>, which is why it can afford to be a
	/// plain conversion: the fragment has already had its half-finished structures removed, so there
	/// is no unclosed fence or partial table row left for the parser to make a mess of.
	/// </para>
	/// </remarks>
	public static string ToHtml(string? content)
		=> string.IsNullOrWhiteSpace(content)
			? string.Empty
			: Markdown.ToHtml(content, _pipeline);

	/// <summary>
	/// Trims a partial markdown fragment to the last point it was structurally complete.
	/// </summary>
	public static Result Trim(string? partial)
	{
		if (string.IsNullOrEmpty(partial))
		{
			return new Result(string.Empty, null);
		}

		var lines = partial.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

		// An unclosed fence swallows everything after it, so it is the most damaging case and is
		// checked first. An odd number of fences means one is still open.
		var fenceCount = lines.Count(line => line.TrimStart().StartsWith("```", StringComparison.Ordinal));

		if (fenceCount % 2 == 1)
		{
			var lastFence = Array.FindLastIndex(lines, line => line.TrimStart().StartsWith("```", StringComparison.Ordinal));

			return new Result(string.Join('\n', lines.Take(lastFence)).TrimEnd(), "code block");
		}

		var lastIndex = lines.Length - 1;
		var lastLine = lines[lastIndex];

		// A table is only renderable once it has a header and a delimiter row, and a row that is
		// still being written renders as a stray cell. Trim to the last complete row.
		if (IsTableLine(lastLine))
		{
			var completeUpTo = lastIndex;

			// The line currently being written is complete only if it ends the row.
			if (!lastLine.TrimEnd().EndsWith('|'))
			{
				completeUpTo = lastIndex - 1;
			}

			// A header with no delimiter row yet is not a table at all.
			var tableStart = completeUpTo;
			while (tableStart > 0 && IsTableLine(lines[tableStart - 1]))
			{
				tableStart--;
			}

			var hasDelimiter = Enumerable
				.Range(tableStart, Math.Max(0, completeUpTo - tableStart + 1))
				.Any(index => TableDelimiterRegex().IsMatch(lines[index]));

			if (!hasDelimiter)
			{
				completeUpTo = tableStart - 1;
			}

			if (completeUpTo < lastIndex)
			{
				return new Result(string.Join('\n', lines.Take(completeUpTo + 1)).TrimEnd(), "table");
			}
		}

		// A heading with nothing after it renders as a dangling title.
		if (HeadingRegex().IsMatch(lastLine))
		{
			return new Result(string.Join('\n', lines.Take(lastIndex)).TrimEnd(), null);
		}

		// A list item mid-word is harmless to render but reads as a truncation; left alone
		// deliberately, because trimming it would make the text visibly jump backwards as it grows.
		return new Result(partial.TrimEnd(), null);
	}

	private static bool IsTableLine(string line)
		=> line.TrimStart().StartsWith('|');

	[GeneratedRegex(@"^\s*\|?[\s:\-|]+\|[\s:\-|]*$")]
	private static partial Regex TableDelimiterRegex();

	[GeneratedRegex(@"^\s{0,3}#{1,6}\s*\S*\s*$")]
	private static partial Regex HeadingRegex();
}
