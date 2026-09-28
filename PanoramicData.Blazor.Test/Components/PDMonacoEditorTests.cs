using AwesomeAssertions;
using BlazorMonaco;
using BlazorMonaco.Editor;
using BlazorMonaco.Languages;
using Bunit;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Models.Monaco;
using Range = BlazorMonaco.Range;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDMonacoEditor"/> configures its Monaco editor, registers languages and completions
/// through its JavaScript module, relays editor events and forwards its commands to the editor.
/// </summary>
/// <remarks>
/// The Monaco editor itself runs only in a browser; here its JavaScript calls are answered by bUnit's
/// JavaScript interop, so these tests pin what the component asks of the editor and what it does with the
/// answers.
/// </remarks>
public class PDMonacoEditorTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDMonacoEditor.razor.js";

	private readonly BunitJSModuleInterop _module;

	/// <summary>Sets up the rendering context, the component's module and the editor's model.</summary>
	public PDMonacoEditorTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
		JSInterop.Setup<TextModel>(invocation => invocation.Identifier.EndsWith("getInstanceModel", StringComparison.Ordinal))
			.SetResult(new TextModel { Id = "model-1", Uri = "inmemory://model/1" });
	}

	/// <summary>The editor is created with the language, theme and value, and InitializeOptions can amend them.</summary>
	[Fact]
	public void The_editor_is_created_with_the_configured_options()
	{
		var component = Render<PDMonacoEditor>(parameters => parameters
			.Add(p => p.Id, "ed")
			.Add(p => p.Language, "sql")
			.Add(p => p.Theme, "vs-dark")
			.Add(p => p.Value, "select 1")
			.Add(p => p.InitializeOptions, options => options.ReadOnly = true));

		var create = JSInterop.VerifyInvoke("blazorMonaco.editor.create");
		create.Arguments[0].Should().Be("ed");
		var options = create.Arguments[1]!.ToString();
		options.Should().Contain("\"language\":\"sql\"").And.Contain("\"theme\":\"vs-dark\"")
			.And.Contain("\"value\":\"select 1\"").And.Contain("\"readOnly\":true").And.Contain("\"automaticLayout\":true");
		component.Find(".pd-monacoeditor").Should().NotBeNull();
		_module.VerifyInvoke("initialize");
	}

	/// <summary>
	/// Registered languages are passed to the module and, once accepted, initialised; languages the module
	/// refuses are not; both cache initialisers then run.
	/// </summary>
	[Fact]
	public void Languages_and_caches_are_initialised_after_first_render()
	{
		_module.Setup<bool>("registerLanguage", invocation => (string?)invocation.Arguments[0] == "accepted").SetResult(true);
		_module.Setup<bool>("registerLanguage", invocation => (string?)invocation.Arguments[0] == "refused").SetResult(false);
		var initialised = new List<string>();
		var caches = new List<string>();
		Render<PDMonacoEditor>(parameters => parameters
			.Add(p => p.RegisterLanguages, languages =>
			{
				languages.Add(new Language { Id = "accepted" });
				languages.Add(new Language { Id = "refused" });
			})
			.Add(p => p.InitializeLanguage, language => initialised.Add($"sync {language.Id}"))
			.Add(p => p.InitializeLanguageAsync, language =>
			{
				initialised.Add($"async {language.Id}");
				return Task.CompletedTask;
			})
			.Add(p => p.InitializeCache, _ => caches.Add("sync"))
			.Add(p => p.InitializeCacheAsync, _ =>
			{
				caches.Add("async");
				return Task.CompletedTask;
			}));

		_module.Invocations["registerLanguage"].Should().HaveCount(2);
		initialised.Should().Equal("sync accepted", "async accepted");
		caches.Should().Equal("sync", "async");
	}

	/// <summary>Completions and signatures come from the method cache for the editor's language, unless suggestions are off.</summary>
	[Fact]
	public void Completions_and_signatures_come_from_the_method_cache()
	{
		var language = $"lang-{Guid.NewGuid():N}";
		var component = Render<PDMonacoEditor>(parameters => parameters
			.Add(p => p.Language, language)
			.Add(p => p.InitializeCache, cache => cache.AddMethod(language, new MethodCache.Method { MethodName = "Sum", Description = "Adds" })));

		var completions = component.Instance.GetCompletions(new Range(1, 1, 1, 1), string.Empty);
		completions.Select(c => c.InsertText).Should().Equal("Sum");
		component.Instance.GetSignatures("Sum").Should().NotBeEmpty();

		component.Render(parameters => parameters.Add(p => p.ShowSuggestions, false));
		component.Instance.GetCompletions(new Range(1, 1, 1, 1), string.Empty).Should().BeEmpty();
		component.Instance.GetSignatures("Sum").Should().BeEmpty();
	}

	/// <summary>ResolveCompletionAsync asks UpdateCacheAsync for the method in the editor's language, and does nothing without it.</summary>
	[Fact]
	public async Task ResolveCompletion_updates_the_cache()
	{
		var requests = new List<string>();
		var component = Render<PDMonacoEditor>(parameters => parameters
			.Add(p => p.Language, "calc")
			.Add(p => p.UpdateCacheAsync, (_, language, method) =>
			{
				requests.Add($"{language}.{method}");
				return Task.CompletedTask;
			}));

		await component.Instance.ResolveCompletionAsync("Max");
		requests.Should().Equal("calc.Max");

		var without = Render<PDMonacoEditor>();
		await FluentActions.Invoking(() => without.Instance.ResolveCompletionAsync("Max")).Should().NotThrowAsync();
	}

	/// <summary>A content change raises ValueChanged with the model's text.</summary>
	[Fact]
	public async Task A_content_change_raises_ValueChanged()
	{
		JSInterop.Setup<string>("blazorMonaco.editor.model.getValue", _ => true).SetResult("typed");
		var values = new List<string>();
		var component = Render<PDMonacoEditor>(parameters => parameters.Add(p => p.ValueChanged, (string v) => values.Add(v)));
		var editor = component.FindComponent<StandaloneCodeEditor>().Instance;

		await component.InvokeAsync(() => editor.OnDidChangeModelContent.InvokeAsync(new ModelContentChangedEvent()));
		await component.InvokeAsync(() => editor.OnDidBlurEditorText.InvokeAsync());

		values.Should().Equal("typed");
	}

	/// <summary>With UpdateValueOnBlur only leaving the editor raises ValueChanged.</summary>
	[Fact]
	public async Task UpdateValueOnBlur_raises_ValueChanged_on_blur_only()
	{
		JSInterop.Setup<string>("blazorMonaco.editor.model.getValue", _ => true).SetResult("blurred");
		var values = new List<string>();
		var component = Render<PDMonacoEditor>(parameters => parameters
			.Add(p => p.UpdateValueOnBlur, true)
			.Add(p => p.ValueChanged, (string v) => values.Add(v)));
		var editor = component.FindComponent<StandaloneCodeEditor>().Instance;

		await component.InvokeAsync(() => editor.OnDidChangeModelContent.InvokeAsync(new ModelContentChangedEvent()));
		await component.InvokeAsync(() => editor.OnDidBlurEditorText.InvokeAsync());

		values.Should().Equal("blurred");
	}

	/// <summary>A selection change in the editor raises SelectionChanged with the new selection.</summary>
	[Fact]
	public async Task A_selection_change_raises_SelectionChanged()
	{
		Selection? received = null;
		var component = Render<PDMonacoEditor>(parameters => parameters.Add(p => p.SelectionChanged, (Selection s) => received = s));
		var editor = component.FindComponent<StandaloneCodeEditor>().Instance;
		var selection = new Selection { StartLineNumber = 2, EndLineNumber = 3 };

		await component.InvokeAsync(() => editor.OnDidChangeCursorSelection.InvokeAsync(new CursorSelectionChangedEvent { Selection = selection }));

		received.Should().BeSameAs(selection);
	}

	/// <summary>The value, a range of it and the selection are read from the editor.</summary>
	[Fact]
	public async Task Values_and_selection_are_read_from_the_editor()
	{
		JSInterop.Setup<string>("blazorMonaco.editor.model.getValue", _ => true).SetResult("all");
		JSInterop.Setup<string>("blazorMonaco.editor.model.getValueInRange", _ => true).SetResult("part");
		JSInterop.Setup<Selection>(invocation => invocation.Identifier.EndsWith("getSelection", StringComparison.Ordinal))
			.SetResult(new Selection { StartLineNumber = 4 });
		var component = Render<PDMonacoEditor>();

		(await component.InvokeAsync(() => component.Instance.GetMonacoValueAsync(EndOfLinePreference.LF, false))).Should().Be("all");
		(await component.InvokeAsync(() => component.Instance.GetMonacoValueAsync(new Range(1, 1, 1, 4), EndOfLinePreference.CRLF))).Should().Be("part");
		(await component.InvokeAsync(component.Instance.GetSelection))!.StartLineNumber.Should().Be(4);
	}

	/// <summary>Commands are forwarded to the editor and its module.</summary>
	[Fact]
	public async Task Commands_are_forwarded_to_the_editor()
	{
		var component = Render<PDMonacoEditor>();
		var editor = component.Instance;

		await component.InvokeAsync(() => editor.SetMonacoValueAsync("new text"));
		await component.InvokeAsync(() => editor.SetSelectionAsync(new Selection(), "test"));
		await component.InvokeAsync(() => editor.UpdateOptions(new EditorUpdateOptions { ReadOnly = true }));
		await component.InvokeAsync(editor.ForceLayoutUpdateAsync);
		await component.InvokeAsync(() => editor.ExecuteEdits("test", [new IdentifiedSingleEditOperation { Text = "x" }]));
		await component.InvokeAsync(() => editor.DisableKeyBindingAsync(13, ctrlKey: true));
		await component.InvokeAsync(() => editor.EnableKeyBindingAsync(13, shiftKey: true));

		var identifiers = JSInterop.Invocations.Select(i => i.Identifier).ToList();
		identifiers.Should().Contain("blazorMonaco.editor.model.setValue")
			.And.Contain("blazorMonaco.editor.setSelection")
			.And.Contain("blazorMonaco.editor.updateOptions")
			.And.Contain("blazorMonaco.editor.layout")
			.And.Contain("blazorMonaco.editor.executeEdits");
		_module.VerifyInvoke("disableKeyBinding").Arguments.Should().Equal(13, true, false, false);
		_module.VerifyInvoke("enableKeyBinding").Arguments.Should().Equal(13, false, false, true);
	}

	/// <summary>A theme change after the editor exists is applied to it once, and an unchanged theme is not re-sent.</summary>
	[Fact]
	public void A_theme_change_updates_the_editor_once()
	{
		var component = Render<PDMonacoEditor>(parameters => parameters.Add(p => p.Theme, "vs"));

		component.Render(parameters => parameters.Add(p => p.Theme, "vs-dark"));
		component.Render(parameters => parameters.Add(p => p.Theme, "vs-dark"));

		JSInterop.Invocations.Count(i => i.Identifier == "blazorMonaco.editor.updateOptions").Should().Be(1);
	}

	/// <summary>A JavaScript failure while applying a theme is swallowed.</summary>
	[Fact]
	public void A_theme_update_failure_is_ignored()
	{
		JSInterop.SetupVoid("blazorMonaco.editor.updateOptions", _ => true).SetException(new JSException("gone"));
		var component = Render<PDMonacoEditor>();

		var act = () => component.Render(parameters => parameters.Add(p => p.Theme, "hc-black"));

		act.Should().NotThrow();
	}

	/// <summary>After disposal the commands and reads do nothing.</summary>
	[Fact]
	public async Task After_disposal_commands_do_nothing()
	{
		var component = Render<PDMonacoEditor>();
		var editor = component.Instance;
		await editor.DisposeAsync();
		var before = JSInterop.Invocations.Count;

		(await editor.GetMonacoValueAsync(EndOfLinePreference.LF, false)).Should().BeEmpty();
		(await editor.GetMonacoValueAsync(new Range(1, 1, 1, 1), EndOfLinePreference.LF)).Should().BeEmpty();
		(await editor.GetSelection()).Should().BeNull();
		await editor.SetMonacoValueAsync("x");
		await editor.SetSelectionAsync(new Selection());
		await editor.UpdateOptions(new EditorUpdateOptions());
		await editor.ForceLayoutUpdateAsync();
		await editor.ExecuteEdits("x", []);
		await editor.DisableKeyBindingAsync(1);
		await editor.EnableKeyBindingAsync(1);

		JSInterop.Invocations.Count.Should().Be(before);
	}
}