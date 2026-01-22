using Microsoft.Extensions.Logging;
using NCalc;
using NCalc.Exceptions;
using PanoramicData.NCalcExtensions;
using System.Text;

namespace PanoramicData.Blazor.Demo.Services;

/// <summary>
/// Enhanced demo implementation of IPDStudioService supporting multiple languages including NCalc.
/// </summary>
public class DemoStudioService(ILogger<DemoStudioService> logger) : IPDStudioService
{
	private const int _defaultTimeoutSeconds = 5;
	private int _currentTimeoutSeconds = _defaultTimeoutSeconds;

	public event EventHandler<StudioExecutionEventArgs>? ExecutionEvent;

	public bool IsExecuting { get; private set; }
	public StudioExecutionStatus CurrentStatus { get; private set; } = StudioExecutionStatus.Ready;

	public IEnumerable<string> GetSupportedLanguages()
		=> ["ncalc", "html", "sql", "mssql", "javascript", "json", "plaintext"];

	public string GetDefaultLanguage() => "ncalc";

	public bool IsLanguageSupported(string language)
		=> GetSupportedLanguages().Contains(language?.ToLowerInvariant());

	public Task<string> ExecuteCodeAsync(
		string code,
		string language,
		CancellationToken cancellationToken)
		=> ExecuteCodeAsync(code, language, null, cancellationToken);

	public Task<string> ExecuteCodeAsync(
		string code,
		string language,
		IProgress<string>? resultsProgress,
		CancellationToken cancellationToken)
		=> ExecuteCodeAsync(code, language, resultsProgress, _defaultTimeoutSeconds, cancellationToken);

	public async Task<string> ExecuteCodeAsync(
		string code,
		string language,
		IProgress<string>? resultsProgress,
		int timeoutSeconds,
		CancellationToken cancellationToken)
	{
		IsExecuting = true;
		_currentTimeoutSeconds = timeoutSeconds > 0 ? timeoutSeconds : _defaultTimeoutSeconds;
		CurrentStatus = language.Equals("ncalc", StringComparison.InvariantCultureIgnoreCase) ? StudioExecutionStatus.StartingNCalc : StudioExecutionStatus.Starting;

		OnExecutionEvent(new StudioExecutionEventArgs
		{
			EventType = StudioExecutionEventType.Started,
			Status = CurrentStatus.ToDisplayString(),
			Timestamp = DateTime.Now
		});

		CancellationTokenSource? timeoutCts = null;

		try
		{
			// Create a timeout cancellation token only if timeout is configured
			if (_currentTimeoutSeconds > 0)
			{
				timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				timeoutCts.CancelAfter(TimeSpan.FromSeconds(_currentTimeoutSeconds));
			}

			var effectiveToken = timeoutCts?.Token ?? cancellationToken;

			// Small delay to show the executing state
			await Task.Delay(language.Equals("ncalc", StringComparison.InvariantCultureIgnoreCase) ? 100 : 1000, effectiveToken);

			CurrentStatus = language.Equals("ncalc", StringComparison.InvariantCultureIgnoreCase) ? StudioExecutionStatus.EvaluatingExpression : StudioExecutionStatus.Processing;
			OnExecutionEvent(new StudioExecutionEventArgs
			{
				EventType = StudioExecutionEventType.Progress,
				Status = CurrentStatus.ToDisplayString(),
				Progress = 0.5
			});

			// Generate output based on language
			var result = await GenerateOutputAsync(code, language, resultsProgress, effectiveToken);

			// Report intermediate results
			resultsProgress?.Report(result);
			OnExecutionEvent(new StudioExecutionEventArgs
			{
				EventType = StudioExecutionEventType.UpdateOutput,
				Output = result,
				Status = "Generating output..."
			});

			if (!language.Equals("ncalc", StringComparison.InvariantCultureIgnoreCase))
			{
				await Task.Delay(500, effectiveToken);
			}

			CurrentStatus = StudioExecutionStatus.Complete;
			OnExecutionEvent(new StudioExecutionEventArgs
			{
				EventType = StudioExecutionEventType.Completed,
				Output = result,
				Status = CurrentStatus.ToDisplayString(),
				IsComplete = true
			});

			logger.LogInformation("Code execution completed for language: {Language}", language);
			return result;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested && (timeoutCts == null || !timeoutCts.IsCancellationRequested))
		{
			// User cancellation
			CurrentStatus = StudioExecutionStatus.Cancelled;
			OnExecutionEvent(new StudioExecutionEventArgs
			{
				EventType = StudioExecutionEventType.Cancelled,
				Status = CurrentStatus.ToDisplayString()
			});
			logger.LogInformation("Code execution was cancelled by user");
			throw;
		}
		catch (OperationCanceledException)
		{
			// Timeout occurred
			CurrentStatus = StudioExecutionStatus.ExecutionTimedOut;
			var timeoutMessage = $"Timed out after {_currentTimeoutSeconds}s";

			// For NCalc, return timeout result but still signal error status
			if (language.Equals("ncalc", StringComparison.InvariantCultureIgnoreCase))
			{
				// Fire error event to set the status correctly
				OnExecutionEvent(new StudioExecutionEventArgs
				{
					EventType = StudioExecutionEventType.Error,
					Status = timeoutMessage,
					Exception = new TimeoutException($"Execution timed out after {_currentTimeoutSeconds} seconds")
				});
				logger.LogWarning("Code execution timed out after {TimeoutSeconds} seconds", _currentTimeoutSeconds);

				// Return timeout content but don't fire completed event
				var timeoutResult = GenerateNCalcTimeoutOutput(_currentTimeoutSeconds);

				// Update output but keep error status
				OnExecutionEvent(new StudioExecutionEventArgs
				{
					EventType = StudioExecutionEventType.UpdateOutput,
					Output = timeoutResult,
					Status = timeoutMessage // Keep timeout status
				});

				return timeoutResult;
			}

			// For non-NCalc languages, fire error event and throw
			OnExecutionEvent(new StudioExecutionEventArgs
			{
				EventType = StudioExecutionEventType.Error,
				Status = timeoutMessage,
				Exception = new TimeoutException($"Execution timed out after {_currentTimeoutSeconds} seconds")
			});
			logger.LogWarning("Code execution timed out after {TimeoutSeconds} seconds", _currentTimeoutSeconds);
			throw new TimeoutException($"Execution timed out after {_currentTimeoutSeconds} seconds");
		}
		catch (ArgumentException ex) when (language.Equals("ncalc", StringComparison.InvariantCultureIgnoreCase))
		{
			// NCalc invalid syntax
			CurrentStatus = StudioExecutionStatus.InvalidCode;
			OnExecutionEvent(new StudioExecutionEventArgs
			{
				EventType = StudioExecutionEventType.Error,
				Status = CurrentStatus.ToDisplayString(),
				Exception = ex
			});
			logger.LogWarning("Invalid NCalc expression: {Message}", ex.Message);
			return GenerateNCalcInvalidCodeOutput(ex, code);
		}
		catch (NCalcEvaluationException ex) when (language.Equals("ncalc", StringComparison.InvariantCultureIgnoreCase))
		{
			// NCalc runtime error during evaluation
			CurrentStatus = StudioExecutionStatus.RuntimeError;
			OnExecutionEvent(new StudioExecutionEventArgs
			{
				EventType = StudioExecutionEventType.Error,
				Status = CurrentStatus.ToDisplayString(),
				Exception = ex
			});
			logger.LogWarning("NCalc runtime error: {Message}", ex.Message);
			return GenerateNCalcRuntimeErrorOutput(ex, code);
		}
		catch (Exception ex)
		{
			CurrentStatus = StudioExecutionStatus.Error;
			OnExecutionEvent(new StudioExecutionEventArgs
			{
				EventType = StudioExecutionEventType.Error,
				Status = $"Error: {ex.Message}",
				Exception = ex
			});
			logger.LogError(ex, "Error executing code");

			// For NCalc, return error result instead of throwing
			if (language.Equals("ncalc", StringComparison.InvariantCultureIgnoreCase))
			{
				return GenerateNCalcErrorOutput(ex, code);
			}

			throw;
		}
		finally
		{
			IsExecuting = false;
			timeoutCts?.Dispose();
		}
	}

	private async Task<string> GenerateOutputAsync(string code, string language, IProgress<string>? resultsProgress, CancellationToken cancellationToken)
		=> language.ToLowerInvariant() switch
		{
			"ncalc" => await EvaluateNCalcExpressionAsync(code, resultsProgress, cancellationToken),
			"html" => code, // Return HTML as-is
			"sql" or "mssql" => GenerateSqlOutput(code),
			"javascript" => GenerateJavaScriptOutput(code),
			"json" => GenerateJsonOutput(code),
			_ => GeneratePlainTextOutput(code, language)
		};

	private async Task<string> EvaluateNCalcExpressionAsync(string code, IProgress<string>? resultsProgress, CancellationToken cancellationToken)
	{
		var resultBuilder = new StringBuilder();
		resultBuilder.AppendLine("<div class='ncalc-results'>");

		try
		{
			// Check for special test cases first
			if (code.Trim().Contains("sleep(", StringComparison.OrdinalIgnoreCase) && code.Contains("10"))
			{
				// Simulate a long-running operation that will timeout
				await Task.Delay(10000, cancellationToken); // 10 seconds - will timeout based on configured timeout
			}

			// Create ExtendedExpression which supports multiline expressions
			var expression = new ExtendedExpression(code);

			CurrentStatus = StudioExecutionStatus.EvaluatingExpression;
			OnExecutionEvent(new StudioExecutionEventArgs
			{
				EventType = StudioExecutionEventType.Progress,
				Status = CurrentStatus.ToDisplayString(),
				Progress = 0.7
			});

			// Add some common functions and variables for demonstration
			AddCommonFunctions(expression);

			// Report intermediate progress
			var intermediateResult = "<div class='success'>Expression parsed successfully</div>";
			resultsProgress?.Report(intermediateResult);

			// Small delay to show progress (unless it's a timeout test)
			if (!code.Trim().Contains("sleep(", StringComparison.OrdinalIgnoreCase))
			{
				await Task.Delay(50, cancellationToken);
			}

			// Evaluate the expression
			var result = expression.Evaluate();

			resultBuilder.AppendLine("<div class='success'>✓ Expression evaluated successfully</div>");
			resultBuilder.AppendLine("<div class='result-section'>");
			resultBuilder.AppendLine("<h4>Result:</h4>");

			if (result != null)
			{
				var resultType = result.GetType();
				var resultValue = FormatResult(result);

				resultBuilder.AppendLine($"<div class='result-value'>{System.Net.WebUtility.HtmlEncode(resultValue)}</div>");
				resultBuilder.AppendLine($"<div class='result-type'>Type: {resultType.Name}</div>");
			}
			else
			{
				resultBuilder.AppendLine("<div class='result-value'>null</div>");
			}

			resultBuilder.AppendLine("</div>");

			// Show expression info
			resultBuilder.AppendLine("<div class='expression-info'>");
			resultBuilder.AppendLine($"<div class='info'>Expression length: {code.Length} characters</div>");
			resultBuilder.AppendLine($"<div class='info'>Evaluation completed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}</div>");
			resultBuilder.AppendLine("</div>");
		}
		catch (OperationCanceledException)
		{
			// Re-throw OperationCanceledException so it can be handled by the main method
			// This allows proper timeout detection and status setting
			throw;
		}
		catch (Exception ex)
		{
			resultBuilder.AppendLine($"<div class='error'>❌ Evaluation Error: {System.Net.WebUtility.HtmlEncode(ex.Message)}</div>");

			if (ex.InnerException != null)
			{
				resultBuilder.AppendLine($"<div class='error-detail'>Inner Exception: {System.Net.WebUtility.HtmlEncode(ex.InnerException.Message)}</div>");
			}
		}

		resultBuilder.AppendLine("</div>");
		return resultBuilder.ToString();
	}

	private static void AddCommonFunctions(ExtendedExpression expression)
	{
		// Add some common variables for demonstration
		expression.Parameters["Pi"] = Math.PI;
		expression.Parameters["E"] = Math.E;
		expression.Parameters["Now"] = DateTime.Now;
		expression.Parameters["Today"] = DateTime.Today;
		expression.Parameters["x"] = 10;
		expression.Parameters["y"] = 20;

		// ExtendedExpression already includes many built-in functions
		// but we can add custom ones if needed
	}

	private static string FormatResult(object result)
		=> result switch
		{
			DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
			decimal dec => dec.ToString("N"),
			double dbl => dbl.ToString("N"),
			float flt => flt.ToString("N"),
			bool boolean => boolean.ToString().ToLower(),
			null => "null",
			_ => result.ToString() ?? "null"
		};

	private static string GenerateNCalcInvalidCodeOutput(Exception ex, string code)
	{
		var errorBuilder = new StringBuilder();
		errorBuilder.AppendLine("<div class='ncalc-results error-results'>");
		errorBuilder.AppendLine($"<div class='error'>❌ Invalid Code: {System.Net.WebUtility.HtmlEncode(ex.Message)}</div>");

		errorBuilder.AppendLine("<div class='error-help'>");
		errorBuilder.AppendLine("<h5>Common Syntax Issues:</h5>");
		errorBuilder.AppendLine("<ul>");
		errorBuilder.AppendLine("<li>Incomplete expressions (e.g., <code>1 +</code>)</li>");
		errorBuilder.AppendLine("<li>Unmatched parentheses</li>");
		errorBuilder.AppendLine("<li>Invalid function names</li>");
		errorBuilder.AppendLine("<li>Missing operands</li>");
		errorBuilder.AppendLine("</ul>");

		errorBuilder.AppendLine("<h5>Valid Examples:</h5>");
		errorBuilder.AppendLine("<ul>");
		errorBuilder.AppendLine("<li><code>2 + 3 * 4</code></li>");
		errorBuilder.AppendLine("<li><code>Sqrt(x * y)</code></li>");
		errorBuilder.AppendLine("<li><code>if(x > 5, 'Greater', 'Lesser')</code></li>");
		errorBuilder.AppendLine("</ul>");
		errorBuilder.AppendLine("</div>");
		errorBuilder.AppendLine("</div>");

		return errorBuilder.ToString();
	}

	private static string GenerateNCalcRuntimeErrorOutput(Exception ex, string code)
	{
		var errorBuilder = new StringBuilder();
		errorBuilder.AppendLine("<div class='ncalc-results error-results'>");
		errorBuilder.AppendLine($"<div class='error'>❌ Runtime Error: {System.Net.WebUtility.HtmlEncode(ex.Message)}</div>");

		errorBuilder.AppendLine("<div class='error-help'>");
		errorBuilder.AppendLine("<h5>Common Runtime Issues:</h5>");
		errorBuilder.AppendLine("<ul>");
		errorBuilder.AppendLine("<li>Division by zero</li>");
		errorBuilder.AppendLine("<li>Invalid function arguments</li>");
		errorBuilder.AppendLine("<li>Type conversion errors</li>");
		errorBuilder.AppendLine("<li>Undefined variables or functions</li>");
		errorBuilder.AppendLine("</ul>");

		errorBuilder.AppendLine("<h5>Available Variables:</h5>");
		errorBuilder.AppendLine("<ul>");
		errorBuilder.AppendLine("<li><code>Pi</code>, <code>E</code>, <code>Now</code>, <code>Today</code></li>");
		errorBuilder.AppendLine("<li><code>x</code> (10), <code>y</code> (20)</li>");
		errorBuilder.AppendLine("</ul>");
		errorBuilder.AppendLine("</div>");
		errorBuilder.AppendLine("</div>");

		return errorBuilder.ToString();
	}

	private static string GenerateNCalcTimeoutOutput(int timeoutSeconds)
	{
		var errorBuilder = new StringBuilder();
		errorBuilder.AppendLine("<div class='ncalc-results error-results'>");
		errorBuilder.AppendLine($"<div class='error'>⏱️ Execution Timed Out: Expression took too long to evaluate (>{timeoutSeconds}s)</div>");

		errorBuilder.AppendLine("<div class='error-help'>");
		errorBuilder.AppendLine("<h5>Possible Causes:</h5>");
		errorBuilder.AppendLine("<ul>");
		errorBuilder.AppendLine("<li>Complex recursive calculations</li>");
		errorBuilder.AppendLine("<li>Infinite loops in expressions</li>");
		errorBuilder.AppendLine("<li>Very large computational operations</li>");
		errorBuilder.AppendLine("<li>Sleep functions (for testing)</li>");
		errorBuilder.AppendLine("</ul>");

		errorBuilder.AppendLine("<h5>Suggestions:</h5>");
		errorBuilder.AppendLine("<ul>");
		errorBuilder.AppendLine("<li>Simplify complex expressions</li>");
		errorBuilder.AppendLine("<li>Break down large calculations</li>");
		errorBuilder.AppendLine("<li>Use smaller input values</li>");
		errorBuilder.AppendLine($"<li>Increase timeout limit (currently {timeoutSeconds}s)</li>");
		errorBuilder.AppendLine("</ul>");
		errorBuilder.AppendLine("</div>");
		errorBuilder.AppendLine("</div>");

		return errorBuilder.ToString();
	}

	private static string GenerateNCalcErrorOutput(Exception ex, string code)
	{
		var errorBuilder = new StringBuilder();
		errorBuilder.AppendLine("<div class='ncalc-results error-results'>");
		errorBuilder.AppendLine($"<div class='error'>❌ Error: {System.Net.WebUtility.HtmlEncode(ex.Message)}</div>");

		if (ex.InnerException != null)
		{
			errorBuilder.AppendLine($"<div class='error-detail'>Details: {System.Net.WebUtility.HtmlEncode(ex.InnerException.Message)}</div>");
		}

		errorBuilder.AppendLine("<div class='error-help'>");
		errorBuilder.AppendLine("<h5>Available Variables:</h5>");
		errorBuilder.AppendLine("<ul>");
		errorBuilder.AppendLine("<li><code>Pi</code> - Mathematical constant π</li>");
		errorBuilder.AppendLine("<li><code>E</code> - Mathematical constant e</li>");
		errorBuilder.AppendLine("<li><code>Now</code> - Current date and time</li>");
		errorBuilder.AppendLine("<li><code>Today</code> - Current date</li>");
		errorBuilder.AppendLine("<li><code>x</code> - Sample variable (10)</li>");
		errorBuilder.AppendLine("<li><code>y</code> - Sample variable (20)</li>");
		errorBuilder.AppendLine("</ul>");
		errorBuilder.AppendLine("<h5>Example Expressions:</h5>");
		errorBuilder.AppendLine("<ul>");
		errorBuilder.AppendLine("<li><code>2 + 3 * 4</code></li>");
		errorBuilder.AppendLine("<li><code>Sqrt(x * y)</code></li>");
		errorBuilder.AppendLine("<li><code>Sin(Pi / 4)</code></li>");
		errorBuilder.AppendLine("<li><code>if(x > 5, 'Greater', 'Lesser')</code></li>");
		errorBuilder.AppendLine("</ul>");
		errorBuilder.AppendLine("</div>");
		errorBuilder.AppendLine("</div>");

		return errorBuilder.ToString();
	}

	private static string GenerateSqlOutput(string sql)
	{
		// Mock SQL execution results
		if (sql.Contains("SUM") || sql.Contains("GROUP BY"))
		{
			return @"
<div class='success'>Query executed successfully</div>
<table>
    <thead>
        <tr><th>Name</th><th>Total Spent</th></tr>
    </thead>
    <tbody>
        <tr><td>John Doe</td><td>$1,234.56</td></tr>
        <tr><td>Jane Smith</td><td>$987.65</td></tr>
        <tr><td>Bob Johnson</td><td>$543.21</td></tr>
    </tbody>
</table>
<div class='info'>3 rows returned in 0.042 seconds</div>";
		}

		return @"
<div class='success'>Query executed successfully</div>
<table>
    <thead>
        <tr><th>Name</th><th>City</th><th>Email</th></tr>
    </thead>
    <tbody>
        <tr><td>John Doe</td><td>London</td><td>john@example.com</td></tr>
        <tr><td>Jane Smith</td><td>London</td><td>jane@example.com</td></tr>
    </tbody>
</table>
<div class='info'>2 rows returned in 0.023 seconds</div>";
	}

	private static string GenerateJavaScriptOutput(string javascript)
	{
		// Mock JavaScript execution output
		if (javascript.Contains("fibonacci"))
		{
			var output = new List<string>();
			for (int i = 0; i < 10; i++)
			{
				var fib = Fibonacci(i);
				output.Add($"<div>fibonacci({i}) = {fib}</div>");
			}

			return $"<div class='success'>JavaScript executed successfully</div><pre>{string.Join("\n", output)}</pre>";
		}

		return $@"
<div class='success'>JavaScript executed successfully</div>
<pre>{System.Net.WebUtility.HtmlEncode(javascript)}</pre>
<div class='info'>Execution completed at {DateTime.Now:HH:mm:ss}</div>";
	}

	private static string GenerateJsonOutput(string json)
		=> $@"
<div class='success'>JSON processed successfully</div>
<pre>{System.Net.WebUtility.HtmlEncode(json)}</pre>
<div class='info'>Valid format</div>";

	private static string GeneratePlainTextOutput(string code, string language)
		=> $@"
<div class='info'>Executed {language} code</div>
<pre>{System.Net.WebUtility.HtmlEncode(code)}</pre>
<div class='info'>Output generated at {DateTime.Now:yyyy-MM-dd HH:mm:ss}</div>";

	private static int Fibonacci(int n)
	{
		if (n <= 1) { return n; }

		int a = 0, b = 1;
		for (int i = 2; i <= n; i++)
		{
			var temp = a + b;
			a = b;
			b = temp;
		}

		return b;
	}

	private void OnExecutionEvent(StudioExecutionEventArgs args)
		=> ExecutionEvent?.Invoke(this, args);
}