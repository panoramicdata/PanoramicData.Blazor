using System.Text.RegularExpressions;
using AwesomeAssertions;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Guards that PDTagInput's colours and sizes can be set from any ancestor, such as a theme on the body.
/// </summary>
public partial class PDTagInputStyleTests
{
	private static string Stylesheet()
		=> File.ReadAllText(Path.Combine(PackagingGuardTests.RepositoryRoot, "PanoramicData.Blazor", "PDTagInput.razor.css"));

	/// <summary>
	/// The component declares none of its own variables, since a declaration on the element would hide a value an
	/// ancestor sets.
	/// </summary>
	[Fact]
	public void The_stylesheet_declares_no_tag_input_variables()
	{
		VariableDeclarationRegex().Matches(Stylesheet()).Select(match => match.Value).Should().BeEmpty();
	}

	/// <summary>Every variable the component reads has a fallback, so it renders the same when nothing sets it.</summary>
	[Fact]
	public void Every_variable_is_read_with_a_fallback()
	{
		var reads = VariableReadRegex().Matches(Stylesheet()).Select(match => match.Value).ToList();

		reads.Should().NotBeEmpty();
		reads.Should().OnlyContain(read => read.Contains(','));
	}

	[GeneratedRegex(@"--pd-taginput-[\w-]+\s*:")]
	private static partial Regex VariableDeclarationRegex();

	[GeneratedRegex(@"var\(--pd-taginput-[\w-]+[^;]*")]
	private static partial Regex VariableReadRegex();
}
