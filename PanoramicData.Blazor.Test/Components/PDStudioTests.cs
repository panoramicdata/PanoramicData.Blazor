using AwesomeAssertions;
using BlazorMonaco.Editor;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDStudio"/>: its layout, running code through its service, cancelling, the service's
/// execution events, the menus and keyboard shortcut.
/// </summary>
/// <remarks>
/// The Monaco editor needs a browser, so code is entered by raising the editor's value change the way the
/// editor itself would.
/// </remarks>
public class PDStudioTests : BunitContext
{
	private readonly FakeStudioService _service = new();

	/// <summary>Sets up the rendering context.</summary>
	public PDStudioTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;

		// The real Monaco editor cannot start without a browser, and calls into it then fail, so the studio is
		// given an editor that keeps every parameter but renders no Monaco instance.
		ComponentFactories.Add<PDMonacoEditor, HeadlessEditor>();
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDStudio> RenderStudio(
		PDStudioOptions? options = null,
		Action<ComponentParameterCollectionBuilder<PDStudio>>? more = null)
		=> Render<PDStudio>(parameters =>
		{
			parameters
				.Add(p => p.StudioService, _service)
				.Add(p => p.Options, options ?? new PDStudioOptions());
			more?.Invoke(parameters);
		});

	private static Task TypeAsync(IRenderedComponent<PDStudio> studio, string code)
	{
		var editor = studio.FindComponent<PDMonacoEditor>().Instance;
		return studio.InvokeAsync(() => editor.ValueChanged.InvokeAsync(code));
	}

	private static AngleSharp.Dom.IElement PlayButton(IRenderedComponent<PDStudio> studio)
		=> studio.Find("button.pdtoolbarbutton");

	private static string Status(IRenderedComponent<PDStudio> studio)
		=> studio.Find(".pd-studio-results-status .status-text").TextContent;

	private static List<string> LogMessages(IRenderedComponent<PDStudio> studio)
		=> [.. studio.FindAll(".log-message").Select(m => m.TextContent)];

	private static Task MenuAsync(IRenderedComponent<PDStudio> studio, string menu, string key)
	{
		var dropdown = studio.FindComponents<PDToolbarDropdown>().Single(d => d.Instance.Text == menu).Instance;
		return studio.InvokeAsync(() => dropdown.Click.InvokeAsync(key));
	}

	/// <summary>
	/// Verifies the default layout: both menus, the Execute button, a ready results panel and a log.
	/// </summary>
	[Fact]
	public void Layout_HasMenusExecuteResultsAndLog()
	{
		var studio = RenderStudio(more: p => p.Add(x => x.CssClass, "my-studio"));

		studio.Find(".pd-studio").ClassList.Should().Contain("my-studio");
		studio.FindComponents<PDToolbarDropdown>().Select(d => d.Instance.Text).Should().Equal("File", "Logging");
		studio.FindComponents<PDToolbarSeparator>().Should().ContainSingle();
		PlayButton(studio).TextContent.Should().Contain("Execute");
		PlayButton(studio).ClassList.Should().Contain("btn-success");
		Status(studio).Should().Be("Ready");
		studio.Instance.LogComponent.Should().NotBeNull();
	}

	/// <summary>
	/// Verifies that the menu, toolbar and log can each be left out, and custom toolbar content is added after a separator.
	/// </summary>
	[Fact]
	public void Layout_PartsCanBeLeftOutOrAdded()
	{
		var withoutMenu = RenderStudio(
			new PDStudioOptions { ShowMenu = false, IsLoggingVisible = false, ShowStatusBar = false },
			p => p.Add(x => x.EditorToolbarContent, b => b.AddMarkupContent(0, "<span class=\"extra\">Extra</span>")));

		withoutMenu.FindComponents<PDToolbarDropdown>().Should().BeEmpty();
		withoutMenu.FindComponents<PDToolbarSeparator>().Should().ContainSingle();
		withoutMenu.Find(".pd-studio-toolbar .extra").TextContent.Should().Be("Extra");
		withoutMenu.FindComponents<PDLog>().Should().BeEmpty();
		withoutMenu.Instance.LogComponent.Should().BeNull();
		withoutMenu.FindAll(".pd-studio-results-status").Should().BeEmpty();

		var withoutToolbar = RenderStudio(new PDStudioOptions { ShowToolbar = false });
		withoutToolbar.FindComponents<PDToolbarDropdown>().Should().HaveCount(2);
		withoutToolbar.FindAll("button.pdtoolbarbutton").Should().BeEmpty();

		var neither = RenderStudio(new PDStudioOptions { ShowToolbar = false, ShowMenu = false });
		neither.FindAll(".pd-studio-toolbar").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that executing runs the code through the service and shows and announces the result.
	/// </summary>
	[Fact]
	public async Task Execute_RunsTheCodeAndShowsTheResult()
	{
		var executed = new List<string>();
		var states = new List<bool>();
		var studio = RenderStudio(new PDStudioOptions { Language = "sql", ExecutionTimeoutSeconds = 7 }, p => p
			.Add(x => x.OnCodeExecuted, code => executed.Add(code))
			.Add(x => x.OnExecutionStateChanged, state => states.Add(state)));
		_service.Result = "<p>42 rows</p>";

		await TypeAsync(studio, "SELECT 1");
		await PlayButton(studio).ClickAsync(new MouseEventArgs());

		studio.WaitForAssertion(() => executed.Should().Equal("SELECT 1"));
		_service.Calls.Should().Equal(("SELECT 1", "sql", 7));
		states.Should().Equal(true);
		studio.Find("iframe.pd-studio-results-iframe").GetAttribute("srcdoc").Should().Contain("<p>42 rows</p>");
		studio.WaitForAssertion(() => LogMessages(studio).Should().Contain("Code execution completed successfully"));
	}

	/// <summary>
	/// Verifies that there is nothing to execute without code, or without a service.
	/// </summary>
	[Fact]
	public async Task Execute_WithoutCodeOrService_DoesNothing()
	{
		var studio = RenderStudio();
		await PlayButton(studio).ClickAsync(new MouseEventArgs());

		var serviceless = Render<PDStudio>();
		await TypeAsync(serviceless, "1 + 1");
		await PlayButton(serviceless).ClickAsync(new MouseEventArgs());

		_service.Calls.Should().BeEmpty();
		Status(serviceless).Should().Be("Ready");
	}

	/// <summary>
	/// Verifies that a failing execution shows the error in the status and the log.
	/// </summary>
	[Fact]
	public async Task Execute_Failure_ShowsTheError()
	{
		var studio = RenderStudio();
		_service.Failure = new InvalidOperationException("boom");

		await TypeAsync(studio, "1 +");
		await PlayButton(studio).ClickAsync(new MouseEventArgs());

		studio.WaitForAssertion(() => Status(studio).Should().Be("Error: boom"));
		LogMessages(studio).Should().Contain("Error executing code: boom");
		PlayButton(studio).TextContent.Should().Contain("Execute");
	}

	/// <summary>
	/// Verifies that while code runs the button offers Cancel and the editor is covered, and cancelling stops it.
	/// </summary>
	[Fact]
	public async Task Cancel_StopsARunningExecution()
	{
		var studio = RenderStudio(new PDStudioOptions { IsEditingEnabledDuringExecution = false });
		_service.Block = true;
		await TypeAsync(studio, "sleep(10)");

		var running = PlayButton(studio).ClickAsync(new());
		studio.WaitForAssertion(() => PlayButton(studio).TextContent.Should().Contain("Cancel"));
		PlayButton(studio).ClassList.Should().Contain("btn-danger");
		studio.FindAll(".pd-studio-editor-overlay").Should().ContainSingle();

		await PlayButton(studio).ClickAsync(new());
		await running;

		studio.WaitForAssertion(() => studio.FindAll(".pd-studio-editor-overlay").Should().BeEmpty());
		PlayButton(studio).TextContent.Should().Contain("Execute");
		LogMessages(studio).Should().Contain("Code execution was cancelled");
	}

	/// <summary>
	/// Verifies that Ctrl+Enter anywhere executes the code, and other keys do not.
	/// </summary>
	[Fact]
	public async Task CtrlEnter_Executes()
	{
		var studio = RenderStudio();
		var events = Services.GetRequiredService<IGlobalEventService>();
		await TypeAsync(studio, "2 * 3");

		events.KeyDown(new KeyboardInfo { Key = "Enter", Code = "Enter" });
		events.KeyDown(new KeyboardInfo { Key = "Enter", Code = "Enter", CtrlKey = true });

		studio.WaitForAssertion(() => _service.Calls.Should().ContainSingle().Which.Code.Should().Be("2 * 3"));
	}

	/// <summary>
	/// Verifies that the service's running state and status are picked up when the studio is given its parameters.
	/// </summary>
	[Fact]
	public void ServiceState_IsPickedUpFromTheService()
	{
		_service.IsExecuting = true;
		_service.CurrentStatus = StudioExecutionStatus.Processing;

		var studio = RenderStudio();

		PlayButton(studio).TextContent.Should().Contain("Cancel");
		studio.FindAll(".pd-studio-results-status .spinner-border").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that after disposal the studio no longer listens to the service.
	/// </summary>
	[Fact]
	public async Task Dispose_StopsListeningToTheService()
	{
		RenderStudio();
		_service.HasListeners.Should().BeTrue();

		await DisposeComponentsAsync();

		_service.HasListeners.Should().BeFalse();
	}

	private async Task RaiseAsync(IRenderedComponent<PDStudio> studio, StudioExecutionEventType type, string status = "", string output = "")
		=> await studio.InvokeAsync(() => _service.Raise(new StudioExecutionEventArgs
		{
			EventType = type,
			Status = status,
			Output = output,
			LogLevel = LogLevel.Warning
		}));

	/// <summary>
	/// Verifies that the service's started, progress, output and completed events drive the status and results.
	/// </summary>
	[Fact]
	public async Task ServiceEvents_DriveTheStatusAndResults()
	{
		var states = new List<bool>();
		var studio = RenderStudio(more: p => p.Add(x => x.OnExecutionStateChanged, s => states.Add(s)));

		await RaiseAsync(studio, StudioExecutionEventType.Started, "Starting up");
		studio.WaitForAssertion(() => Status(studio).Should().Be("Starting up"));
		PlayButton(studio).TextContent.Should().Contain("Cancel");

		await RaiseAsync(studio, StudioExecutionEventType.Progress, "Halfway");
		studio.WaitForAssertion(() => Status(studio).Should().Be("Halfway"));

		await RaiseAsync(studio, StudioExecutionEventType.UpdateOutput, output: "<b>partial</b>");
		studio.WaitForAssertion(() => studio.Find("iframe").GetAttribute("srcdoc").Should().Contain("<b>partial</b>"));
		Status(studio).Should().Be("Halfway");

		await RaiseAsync(studio, StudioExecutionEventType.UpdateOutput, "Timed out after 5s", "<b>final</b>");
		studio.WaitForAssertion(() => Status(studio).Should().Be("Timed out after 5s"));

		await RaiseAsync(studio, StudioExecutionEventType.OutputComplete, "ignored");
		await RaiseAsync(studio, StudioExecutionEventType.Completed, "done");
		studio.WaitForAssertion(() => Status(studio).Should().Be("Complete"));
		states.Should().Equal(false);
		PlayButton(studio).TextContent.Should().Contain("Execute");
	}

	/// <summary>
	/// Verifies that a cancelled event ends the execution with a cancelled status.
	/// </summary>
	[Fact]
	public async Task ServiceEvents_Cancelled_EndsTheExecution()
	{
		var studio = RenderStudio(more: p => p.Add(x => x.OnExecutionStateChanged, _ => { }));

		await RaiseAsync(studio, StudioExecutionEventType.Started, "Starting");
		await RaiseAsync(studio, StudioExecutionEventType.Cancelled, "by user");

		studio.WaitForAssertion(() => Status(studio).Should().Be("Cancelled"));
		studio.WaitForAssertion(() => LogMessages(studio).Should().Contain("Execution Cancelled: by user"));
	}

	/// <summary>
	/// Verifies that an error event ends the execution, keeping a timeout message and prefixing any other.
	/// </summary>
	[Theory]
	[InlineData("Timed out after 30s", "Timed out after 30s")]
	[InlineData("Division by zero", "Error: Division by zero")]
	public async Task ServiceEvents_Error_EndsTheExecution(string message, string expected)
	{
		var states = new List<bool>();
		var studio = RenderStudio(more: p => p.Add(x => x.OnExecutionStateChanged, s => states.Add(s)));

		await RaiseAsync(studio, StudioExecutionEventType.Started, "Starting");
		await RaiseAsync(studio, StudioExecutionEventType.Error, message);

		studio.WaitForAssertion(() => Status(studio).Should().Be(expected));
		states.Should().Equal(false);
		LogMessages(studio).Should().Contain($"Execution error: {message}");
	}

	/// <summary>
	/// Verifies that a log event from the service is written to the log panel.
	/// </summary>
	[Fact]
	public async Task ServiceEvents_Log_IsWrittenToTheLog()
	{
		var studio = RenderStudio();

		await RaiseAsync(studio, StudioExecutionEventType.Log, "Parsed 3 tokens");

		studio.WaitForAssertion(() => LogMessages(studio).Should().Contain("Parsed 3 tokens"));
		studio.Find(".log-entry i").ClassList.Should().Contain("text-warning");
	}

	/// <summary>
	/// Verifies that the examples on the File menu load code suited to the studio's language.
	/// </summary>
	[Theory]
	[InlineData("sql", "LoadExample1", "SELECT * FROM customers")]
	[InlineData("mssql", "LoadExample2", "GROUP BY c.id")]
	[InlineData("ncalc", "LoadExample1", "Basic NCalc Expression Examples")]
	[InlineData("ncalc", "LoadExample2", "Test Error Scenarios")]
	[InlineData("html", "LoadExample1", "<h1>Hello World</h1>")]
	[InlineData("html", "LoadExample2", "Sample Table")]
	[InlineData("javascript", "LoadExample1", "console.log('Hello World!')")]
	[InlineData("javascript", "LoadExample2", "function fibonacci")]
	[InlineData("python", "LoadExample1", "Example 1 for python")]
	[InlineData("python", "LoadExample2", "Example 2 for python")]
	public async Task FileMenu_ExamplesSuitTheLanguage(string language, string example, string expected)
	{
		var studio = RenderStudio(new PDStudioOptions { Language = language });

		await MenuAsync(studio, "File", example);
		await PlayButton(studio).ClickAsync(new MouseEventArgs());

		studio.WaitForAssertion(() => _service.Calls.Should().ContainSingle().Which.Code.Should().Contain(expected));
	}

	/// <summary>
	/// Verifies that New clears the code and results, so there is then nothing to execute.
	/// </summary>
	[Fact]
	public async Task FileMenu_New_ClearsTheCode()
	{
		var studio = RenderStudio();
		await TypeAsync(studio, "<p>Hi</p>");

		await MenuAsync(studio, "File", "New");
		await PlayButton(studio).ClickAsync(new MouseEventArgs());

		_service.Calls.Should().BeEmpty();
		studio.WaitForAssertion(() => LogMessages(studio).Should().Contain("New document created"));
		studio.FindAll(".pd-studio-results-placeholder").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that Open and Save, not yet implemented, only log that they were chosen, and unknown keys do nothing.
	/// </summary>
	[Fact]
	public async Task FileMenu_OpenAndSave_OnlyLog()
	{
		var studio = RenderStudio();

		await MenuAsync(studio, "File", "Open");
		await MenuAsync(studio, "File", "Save");
		await MenuAsync(studio, "File", "Unknown");

		studio.WaitForAssertion(() => LogMessages(studio).Should()
			.Equal("Open clicked (not implemented)", "Save clicked (not implemented)"));
	}

	/// <summary>
	/// Verifies that the Logging menu sets the studio's default log level, treating unknown keys as Information.
	/// </summary>
	[Theory]
	[InlineData("Debug", LogLevel.Debug)]
	[InlineData("Info", LogLevel.Information)]
	[InlineData("Warning", LogLevel.Warning)]
	[InlineData("Error", LogLevel.Error)]
	[InlineData("Verbose", LogLevel.Information)]
	public async Task LoggingMenu_SetsTheLogLevel(string key, LogLevel expected)
	{
		var options = new PDStudioOptions { DefaultLogLevel = LogLevel.Critical };
		var studio = RenderStudio(options);

		await MenuAsync(studio, "Logging", key);

		options.DefaultLogLevel.Should().Be(expected);
		studio.WaitForAssertion(() => LogMessages(studio).Should().Contain($"Log level changed to {expected}"));
	}

	/// <summary>
	/// Verifies that the editor is configured with the studio's defaults, then the caller's own adjustments.
	/// </summary>
	[Fact]
	public void EditorOptions_ApplyTheDefaultsThenTheCallersAdjustments()
	{
		var studio = RenderStudio(more: p => p
			.Add(x => x.InitializeMonacoOptions, o => o.WordWrap = "off"));
		var options = new StandaloneEditorConstructionOptions();

		studio.FindComponent<PDMonacoEditor>().Instance.InitializeOptions!(options);

		options.AutomaticLayout.Should().BeTrue();
		options.ScrollBeyondLastLine.Should().BeFalse();
		options.Minimap!.Enabled.Should().BeFalse();
		options.Folding.Should().BeTrue();
		options.WordWrap.Should().Be("off");
	}

	/// <summary>
	/// Verifies that the editor is given the studio's language and theme.
	/// </summary>
	[Fact]
	public void Editor_UsesTheLanguageAndTheme()
	{
		var studio = RenderStudio(new PDStudioOptions { Language = "sql", Theme = "vs-dark" });

		var editor = studio.FindComponent<PDMonacoEditor>().Instance;
		editor.Language.Should().Be("sql");
		editor.Theme.Should().Be("vs-dark");
	}

	private sealed class HeadlessEditor : PDMonacoEditor
	{
		protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
			=> builder.AddMarkupContent(0, "<div class=\"headless-editor\"></div>");
	}

	private sealed class FakeStudioService : IPDStudioService
	{
		public event EventHandler<StudioExecutionEventArgs>? ExecutionEvent;

		public List<(string Code, string Language, int Timeout)> Calls { get; } = [];

		public string Result { get; set; } = "<p>Done</p>";

		public Exception? Failure { get; set; }

		public bool Block { get; set; }

		public bool IsExecuting { get; set; }

		public StudioExecutionStatus CurrentStatus { get; set; } = StudioExecutionStatus.Ready;

		public bool HasListeners => ExecutionEvent is not null;

		public void Raise(StudioExecutionEventArgs args) => ExecutionEvent?.Invoke(this, args);

		public IEnumerable<string> GetSupportedLanguages() => ["html"];

		public string GetDefaultLanguage() => "html";

		public bool IsLanguageSupported(string language) => GetSupportedLanguages().Contains(language);

		public Task<string> ExecuteCodeAsync(string code, string language, CancellationToken cancellationToken)
			=> ExecuteCodeAsync(code, language, null, 0, cancellationToken);

		public Task<string> ExecuteCodeAsync(string code, string language, IProgress<string>? resultsProgress, CancellationToken cancellationToken)
			=> ExecuteCodeAsync(code, language, resultsProgress, 0, cancellationToken);

		public async Task<string> ExecuteCodeAsync(string code, string language, IProgress<string>? resultsProgress, int timeoutSeconds, CancellationToken cancellationToken)
		{
			Calls.Add((code, language, timeoutSeconds));
			resultsProgress?.Report(code);
			if (Block)
			{
				await Task.Delay(Timeout.Infinite, cancellationToken);
			}

			cancellationToken.ThrowIfCancellationRequested();
			return Failure is null ? Result : throw Failure;
		}
	}
}
