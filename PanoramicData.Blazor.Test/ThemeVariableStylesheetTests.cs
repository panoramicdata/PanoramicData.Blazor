using AwesomeAssertions;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// The tree, combo box and context menu take their colours from CSS variables a host can set, and each default is the
/// colour the component had before, so a host that sets nothing sees no change.
/// </summary>
public class ThemeVariableStylesheetTests
{
	/// <summary>Each themable colour reads its variable, falling back to the component's original colour.</summary>
	/// <param name="stylesheet">The component stylesheet.</param>
	/// <param name="declaration">The declaration it must contain.</param>
	[Theory]
	[InlineData("PDTreeNode.razor.css", "background-color: var(--pd-tree-selected-bg, #007bff);")]
	[InlineData("PDTreeNode.razor.css", "color: var(--pd-tree-selected-color, inherit);")]
	[InlineData("PDTreeNode.razor.css", "margin-right: var(--pd-tree-node-inset-end, 0);")]
	[InlineData("PDTreeNode.razor.css", "border-radius: var(--pd-tree-node-radius, 0);")]
	[InlineData("PDComboBox.razor.css", "border-color: var(--pdcb-focus-border, #007bff);")]
	[InlineData("PDComboBox.razor.css", "box-shadow: 0 0 0 0.2rem var(--pdcb-focus-ring, rgba(0, 123, 255, 0.25));")]
	[InlineData("PDContextMenu.razor.css", "background-color: var(--pd-contextmenu-bg, #fff);")]
	[InlineData("PDContextMenu.razor.css", "background-color: var(--pd-contextmenu-hover-bg, #eee);")]
	[InlineData("PDContextMenu.razor.css", "color: var(--pd-contextmenu-disabled-color, #6c757d);")]
	[InlineData("PDContextMenu.razor.css", "border-bottom: 1px solid var(--pd-contextmenu-border-color, rgba(0,0,0,.125));")]
	[InlineData("PDFormFieldEditor.razor.css", "border-top: 1px solid var(--pd-form-editor-border-color, #ced4da);")]
	public void A_colour_reads_its_variable_with_the_original_default(string stylesheet, string declaration)
		=> Read(stylesheet).Should().Contain(declaration);

	/// <summary>No colour in the context menu's stylesheet is fixed, so a host can theme the whole menu.</summary>
	[Fact]
	public void Every_context_menu_colour_reads_a_variable()
	{
		var colourLines = Read("PDContextMenu.razor.css")
			.Split('\n')
			.Select(line => line.Trim())
			.Where(line => line.StartsWith("background-color:", StringComparison.Ordinal)
				|| line.StartsWith("color:", StringComparison.Ordinal)
				|| line.StartsWith("border-color:", StringComparison.Ordinal)
				|| (line.StartsWith("border", StringComparison.Ordinal) && line.Contains("solid", StringComparison.Ordinal)));

		colourLines.Should().NotBeEmpty().And.AllSatisfy(line => line.Should().Contain("var(--pd-contextmenu-"));
	}

	private static string Read(string stylesheet)
		=> File.ReadAllText(Path.Combine(PackagingGuardTests.RepositoryRoot, "PanoramicData.Blazor", stylesheet));
}
