using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Code execution and cancellation tests for <see cref="PDStudio"/>, and the studio service double every test
/// renders the studio with.
/// </summary>
public partial class PDStudioTests
{
	private readonly FakeStudioService _service = new();

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
	/// Verifies that once a cancel has completed the status reads "Cancelled", not "Cancelling..." (#176).
	/// </summary>
	[Fact]
	public async Task Cancel_WhenComplete_StatusIsCancelled()
	{
		var studio = RenderStudio();
		_service.Block = true;
		await TypeAsync(studio, "sleep(10)");

		var running = PlayButton(studio).ClickAsync(new());
		studio.WaitForAssertion(() => PlayButton(studio).TextContent.Should().Contain("Cancel"));

		await PlayButton(studio).ClickAsync(new());
		await running;

		PlayButton(studio).TextContent.Should().Contain("Execute");
		Status(studio).Should().Be("Cancelled");
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

	private sealed class FakeStudioService : IPDStudioService
	{
		private EventHandler<StudioExecutionEventArgs>? _executionEvent;

		public event EventHandler<StudioExecutionEventArgs>? ExecutionEvent
		{
			add => _executionEvent += value;
			remove => _executionEvent -= value;
		}

		public List<(string Code, string Language, int Timeout)> Calls { get; } = [];

		public string Result { get; set; } = "<p>Done</p>";

		public Exception? Failure { get; set; }

		public bool Block { get; set; }

		public bool IsExecuting { get; set; }

		public StudioExecutionStatus CurrentStatus { get; set; } = StudioExecutionStatus.Ready;

		public bool HasListeners => _executionEvent is not null;

		public void Raise(StudioExecutionEventArgs args) => _executionEvent?.Invoke(this, args);

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
