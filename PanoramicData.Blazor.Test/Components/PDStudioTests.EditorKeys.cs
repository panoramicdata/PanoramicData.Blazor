using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Keys a host leaves to the page, so its shortcuts work from inside the code editor.
/// </summary>
public partial class PDStudioTests
{
	/// <summary>Verifies that a shortcut's letter, digit or function key gives the editor's key code.</summary>
	[Theory]
	[InlineData("ctrl-alt-KeyO", 79)]
	[InlineData("ctrl-alt-o", 79)]
	[InlineData("ctrl-Digit1", 49)]
	[InlineData("ctrl-5", 53)]
	[InlineData("ctrl-F5", 116)]
	[InlineData("F12", 123)]
	public void AShortcutsKey_GivesTheEditorsKeyCode(string shortcut, int expected)
		=> PDStudio.EditorKeyCode(ShortcutKey.Create(shortcut)).Should().Be(expected);

	/// <summary>Verifies that a shortcut whose key is not a letter, digit or function key gives no key code.</summary>
	[Theory]
	[InlineData("ctrl-Quote")]
	[InlineData("ctrl-F13")]
	[InlineData("ctrl-Enter")]
	public void AShortcutWithAnotherKey_GivesNoKeyCode(string shortcut)
		=> PDStudio.EditorKeyCode(ShortcutKey.Create(shortcut)).Should().BeNull();

	/// <summary>
	/// Verifies that a shortcut the host leaves to the page is taken from the editor, as Ctrl+Enter is, once the editor has
	/// started, even when asked for before it had.
	/// </summary>
	[Fact]
	public async Task AShortcutLeftToThePage_IsTakenFromTheEditor_OnceItHasStarted()
	{
		ComponentFactories.Clear();
		var module = JSInterop.SetupModule("./_content/PanoramicData.Blazor/PDMonacoEditor.razor.js");
		module.Setup<bool>("waitForMonaco", _ => true).SetResult(true);
		var studio = RenderStudio();
		await studio.InvokeAsync(() => studio.Instance.DisableEditorKeyAsync(ShortcutKey.Create("ctrl-alt-KeyO")));
		studio.WaitForState(() => studio.FindComponents<BlazorMonaco.Editor.StandaloneCodeEditor>().Count > 0);

		await studio.InvokeAsync(() => studio.FindComponent<BlazorMonaco.Editor.StandaloneCodeEditor>().Instance.OnDidInit.InvokeAsync());

		module.Invocations["disableKeyBinding"].Select(i => i.Arguments).Should().ContainEquivalentOf(new object[] { 79, true, true, false });
	}

	/// <summary>Verifies that a shortcut with a key the editor has no code for is left alone, rather than failing.</summary>
	[Fact]
	public async Task AShortcutWithAnotherKey_IsLeftAsItIs()
	{
		var studio = RenderStudio();

		var leaving = () => studio.InvokeAsync(() => studio.Instance.DisableEditorKeyAsync(ShortcutKey.Create("ctrl-Quote")));

		await leaving.Should().NotThrowAsync();
	}
}
