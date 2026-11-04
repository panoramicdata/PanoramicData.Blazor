using AwesomeAssertions;
using BlazorMonaco.Editor;
using Bunit;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDStudio.UpdateEditorOptionsAsync"/> reaches the studio's real code editor, whose
/// JavaScript calls bUnit answers.
/// </summary>
public class PDStudioEditorOptionsTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDMonacoEditor.razor.js";

	/// <summary>Sets up the rendering context and the editor's module.</summary>
	public PDStudioEditorOptionsTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule(ModulePath).Setup<bool>("waitForMonaco", _ => true).SetResult(true);
		Services.AddPanoramicDataBlazor();
	}

	private List<string> OptionsSent()
		=> [.. JSInterop.Invocations.Where(i => i.Identifier == "blazorMonaco.editor.updateOptions").Select(i => i.Arguments[1]!.ToString()!)];

	// BlazorMonaco raises OnDidInit once the browser has created the editor.
	private static Task EditorCreatedAsync(IRenderedComponent<PDStudio> studio)
		=> studio.InvokeAsync(() => studio.FindComponent<StandaloneCodeEditor>().Instance.OnDidInit.InvokeAsync());

	/// <summary>Verifies that options given while the studio is open reach its code editor.</summary>
	[Fact]
	public async Task UpdateEditorOptions_ReachTheCodeEditor()
	{
		var studio = Render<PDStudio>();
		await EditorCreatedAsync(studio);

		await studio.InvokeAsync(() => studio.Instance.UpdateEditorOptionsAsync(new EditorUpdateOptions { TabSize = 2, InsertSpaces = true }));

		OptionsSent().Should().ContainSingle().Which.Should().Contain("\"tabSize\":2").And.Contain("\"insertSpaces\":true");
	}

	/// <summary>Verifies that options given before the code editor has started reach it once it has.</summary>
	[Fact]
	public async Task UpdateEditorOptions_GivenBeforeTheEditorHasStarted_ReachItOnceItHas()
	{
		var create = JSInterop.SetupVoid("blazorMonaco.editor.create", _ => true);
		var studio = Render<PDStudio>();

		await studio.InvokeAsync(() => studio.Instance.UpdateEditorOptionsAsync(new EditorUpdateOptions { ReadOnly = true }));
		create.SetVoidResult();

		studio.WaitForAssertion(() => OptionsSent().Should().NotBeEmpty().And.AllSatisfy(sent => sent.Should().Contain("\"readOnly\":true")));
	}
}
