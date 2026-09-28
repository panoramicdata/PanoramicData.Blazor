using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDTextArea"/> renders its attributes, raises value and key events, honours the
/// debounce setting and drives its JavaScript modules.
/// </summary>
public class PDTextAreaTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDTextArea.razor.js";

	private readonly BunitJSModuleInterop _module;
	private readonly BunitJSModuleInterop _commonModule;

	/// <summary>Sets up the rendering context and both JavaScript modules.</summary>
	public PDTextAreaTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
		_commonModule = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
	}

	/// <summary>
	/// Verifies that the parameters reach the textarea element.
	/// </summary>
	[Fact]
	public void Parameters_AreRenderedOnTheTextArea()
	{
		var component = Render<PDTextArea>(parameters => parameters
			.Add(p => p.Value, "hello")
			.Add(p => p.CssClass, "extra")
			.Add(p => p.Placeholder, "Type here")
			.Add(p => p.Rows, 8)
			.Add(p => p.MaxLength, 200)
			.Add(p => p.IsReadOnly, true));

		var textarea = component.Find("textarea");
		textarea.Id.Should().Be(component.Instance.Id).And.StartWith("pd-textarea-");
		textarea.ClassList.Should().Contain(["form-control", "extra"]);
		textarea.ClassList.Should().NotContain("d-none");
		textarea.GetAttribute("placeholder").Should().Be("Type here");
		textarea.GetAttribute("rows").Should().Be("8");
		textarea.GetAttribute("maxlength").Should().Be("200");
		textarea.HasAttribute("readonly").Should().BeTrue();
		textarea.HasAttribute("disabled").Should().BeFalse();
		textarea.GetAttribute("value").Should().Be("hello");
	}

	/// <summary>
	/// Verifies that an invisible, disabled text area is hidden and disabled.
	/// </summary>
	[Fact]
	public void InvisibleAndDisabled_AreRendered()
	{
		var component = Render<PDTextArea>(parameters => parameters
			.Add(p => p.IsVisible, false)
			.Add(p => p.IsEnabled, false));

		var textarea = component.Find("textarea");
		textarea.ClassList.Should().Contain("d-none");
		textarea.HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>
	/// Verifies that without a debounce every input raises <see cref="PDTextArea.ValueChanged"/> immediately,
	/// and that only the text area module is initialised.
	/// </summary>
	[Fact]
	public void Input_WithoutDebounce_RaisesValueChanged()
	{
		var values = new List<string>();
		var component = Render<PDTextArea>(parameters => parameters
			.Add(p => p.ValueChanged, (string v) => values.Add(v)));

		component.Find("textarea").Input("abc");

		values.Should().Equal("abc");
		component.Instance.Value.Should().Be("abc");
		_module.VerifyInvoke("initTextArea").Arguments[0].Should().Be(component.Instance.Id);
		_commonModule.VerifyNotInvoke("debounceInput");
	}

	/// <summary>
	/// Verifies that with a debounce the input event does not raise a change, and debouncing is set up in
	/// JavaScript with the configured wait.
	/// </summary>
	[Fact]
	public void Input_WithDebounce_DefersToJavaScript()
	{
		var values = new List<string>();
		var component = Render<PDTextArea>(parameters => parameters
			.Add(p => p.DebounceWait, 250)
			.Add(p => p.ValueChanged, (string v) => values.Add(v)));

		component.Find("textarea").Input("abc");

		values.Should().BeEmpty();
		var debounce = _commonModule.VerifyInvoke("debounceInput");
		debounce.Arguments[0].Should().Be(component.Instance.Id);
		debounce.Arguments[1].Should().Be(250);
	}

	/// <summary>
	/// Verifies that a debounced value from JavaScript updates the value and raises the change, unless a blur
	/// has just taken the value already, in which case the one debounced update is ignored.
	/// </summary>
	[Fact]
	public async Task DebouncedInput_IsIgnoredOnceAfterBlur()
	{
		_commonModule.Setup<string>("getValue", _ => true).SetResult("from blur");
		var values = new List<string>();
		var blurs = 0;
		var component = Render<PDTextArea>(parameters => parameters
			.Add(p => p.DebounceWait, 250)
			.Add(p => p.ValueChanged, (string v) => values.Add(v))
			.Add(p => p.Blur, () => blurs++));

		await component.InvokeAsync(() => component.Instance.OnDebouncedInput("first"));
		component.Instance.Value.Should().Be("first");

		component.Find("textarea").Blur();
		await component.InvokeAsync(() => component.Instance.OnDebouncedInput("stale"));
		await component.InvokeAsync(() => component.Instance.OnDebouncedInput("second"));

		values.Should().Equal("first", "from blur", "second");
		blurs.Should().Be(1);
	}

	/// <summary>
	/// Verifies that without a debounce a blur only raises <see cref="PDTextArea.Blur"/>, and a debounced update
	/// from JavaScript is ignored.
	/// </summary>
	[Fact]
	public async Task Blur_WithoutDebounce_OnlyRaisesBlur()
	{
		var values = new List<string>();
		var blurs = 0;
		var component = Render<PDTextArea>(parameters => parameters
			.Add(p => p.ValueChanged, (string v) => values.Add(v))
			.Add(p => p.Blur, () => blurs++));

		component.Find("textarea").Blur();
		await component.InvokeAsync(() => component.Instance.OnDebouncedInput("ignored"));

		blurs.Should().Be(1);
		values.Should().BeEmpty();
		_commonModule.VerifyNotInvoke("getValue");
	}

	/// <summary>
	/// Verifies that a key press without a debounce raises the current value and then the key press.
	/// </summary>
	[Fact]
	public void Keypress_WithoutDebounce_RaisesValueAndKey()
	{
		var values = new List<string>();
		var keys = new List<string>();
		var component = Render<PDTextArea>(parameters => parameters
			.Add(p => p.Value, "abc")
			.Add(p => p.ValueChanged, (string v) => values.Add(v))
			.Add(p => p.Keypress, (KeyboardEventArgs e) => keys.Add(e.Key)));

		component.Find("textarea").KeyPress(new KeyboardEventArgs { Key = "x" });

		values.Should().Equal("abc");
		keys.Should().Equal("x");
	}

	/// <summary>
	/// Verifies that a key press with a debounce raises nothing.
	/// </summary>
	[Fact]
	public void Keypress_WithDebounce_RaisesNothing()
	{
		var raised = 0;
		var component = Render<PDTextArea>(parameters => parameters
			.Add(p => p.DebounceWait, 100)
			.Add(p => p.ValueChanged, (string _) => raised++)
			.Add(p => p.Keypress, (KeyboardEventArgs _) => raised++));

		component.Find("textarea").KeyPress(new KeyboardEventArgs { Key = "x" });

		raised.Should().Be(0);
	}

	/// <summary>
	/// Verifies that a selection report from JavaScript is stored and raised, and that a repeat of the same
	/// range is not raised again.
	/// </summary>
	[Fact]
	public async Task SelectionChanged_IsRaisedOnlyWhenRangeChanges()
	{
		var selections = new List<TextAreaSelection>();
		var component = Render<PDTextArea>(parameters => parameters
			.Add(p => p.SelectionChanged, (TextAreaSelection s) => selections.Add(s)));

		await component.InvokeAsync(() => component.Instance.OnSelectionChanged(2, 5, "llo"));
		await component.InvokeAsync(() => component.Instance.OnSelectionChanged(2, 5, "llo"));

		selections.Should().ContainSingle();
		var selection = component.Instance.GetSelection();
		selection.Start.Should().Be(2);
		selection.End.Should().Be(5);
		selection.Value.Should().Be("llo");
	}

	/// <summary>
	/// Verifies that the selection, value and scroll methods call the matching JavaScript functions.
	/// </summary>
	[Fact]
	public async Task PublicMethods_CallJavaScript()
	{
		var component = Render<PDTextArea>();
		var id = component.Instance.Id;

		await component.InvokeAsync(() => component.Instance.SetSelectionAsync(1, 3));
		await component.InvokeAsync(() => component.Instance.SetValueAsync("new"));
		await component.InvokeAsync(() => component.Instance.ScrollToEndAsync());

		_module.VerifyInvoke("setSelection").Arguments.Should().Equal(id, 1, 3);
		_commonModule.VerifyInvoke("setValue").Arguments.Should().Equal(id, "new");
		_commonModule.VerifyInvoke("scrollToEnd").Arguments.Should().Equal(id);
	}

	/// <summary>
	/// Verifies that when the modules could not be imported the public methods do nothing rather than throw.
	/// </summary>
	[Fact]
	public async Task PublicMethods_WithoutModules_DoNothing()
	{
		var runtime = new ImportFailingJsRuntime();
		Services.AddSingleton<IJSRuntime>(runtime);
		var component = Render<PDTextArea>();

		await component.InvokeAsync(() => component.Instance.SetSelectionAsync(1, 3));
		await component.InvokeAsync(() => component.Instance.SetValueAsync("new"));
		await component.InvokeAsync(() => component.Instance.ScrollToEndAsync());
		await component.InvokeAsync(component.Instance.DisposeAsync().AsTask);

		runtime.Identifiers.Should().Equal("import");
	}

	/// <summary>
	/// A JavaScript runtime whose module imports always fail, as they can when a page is torn down mid-render.
	/// </summary>
	private sealed class ImportFailingJsRuntime : IJSRuntime
	{
		public List<string> Identifiers { get; } = [];

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
			=> InvokeAsync<TValue>(identifier, CancellationToken.None, args);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Identifiers.Add(identifier);
			throw new JSException($"Cannot {identifier} {args?.Length ?? 0} argument(s).");
		}
	}

	/// <summary>
	/// Verifies that disposing the component tells the text area module to release the element.
	/// </summary>
	[Fact]
	public async Task Dispose_TerminatesTheTextArea()
	{
		var component = Render<PDTextArea>();
		var id = component.Instance.Id;

		await DisposeComponentsAsync();

		_module.VerifyInvoke("termTextArea").Arguments.Should().Equal(id);
	}

	/// <summary>
	/// Verifies that the enable and disable methods toggle the disabled attribute.
	/// </summary>
	[Fact]
	public async Task EnableDisable_ToggleDisabledAttribute()
	{
		var component = Render<PDTextArea>();

		await component.InvokeAsync(component.Instance.Disable);
		component.Find("textarea").HasAttribute("disabled").Should().BeTrue();

		await component.InvokeAsync(component.Instance.Enable);
		component.Find("textarea").HasAttribute("disabled").Should().BeFalse();

		await component.InvokeAsync(() => component.Instance.SetEnabled(false));
		component.Find("textarea").HasAttribute("disabled").Should().BeTrue();
		component.Instance.IsEnabled.Should().BeFalse();
	}
}
