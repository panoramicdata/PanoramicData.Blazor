using AwesomeAssertions;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Extensions;

/// <summary>Tests for <see cref="StudioExecutionStatusExtensions"/>.</summary>
public class StudioExecutionStatusExtensionsTests
{
	/// <summary>Each status has a human-readable description.</summary>
	[Theory]
	[InlineData(StudioExecutionStatus.Ready, "Ready")]
	[InlineData(StudioExecutionStatus.Starting, "Starting execution...")]
	[InlineData(StudioExecutionStatus.StartingNCalc, "Starting NCalc evaluation...")]
	[InlineData(StudioExecutionStatus.Processing, "Processing...")]
	[InlineData(StudioExecutionStatus.ParsingExpression, "Parsing expression...")]
	[InlineData(StudioExecutionStatus.EvaluatingExpression, "Evaluating expression...")]
	[InlineData(StudioExecutionStatus.GeneratingOutput, "Generating output...")]
	[InlineData(StudioExecutionStatus.Complete, "Complete")]
	[InlineData(StudioExecutionStatus.Cancelled, "Cancelled")]
	[InlineData(StudioExecutionStatus.Error, "Error")]
	[InlineData(StudioExecutionStatus.Cancelling, "Cancelling...")]
	[InlineData(StudioExecutionStatus.InvalidCode, "Invalid code")]
	[InlineData(StudioExecutionStatus.ExecutionTimedOut, "Execution timed out")]
	[InlineData(StudioExecutionStatus.RuntimeError, "Runtime error")]
	public void ToDisplayString_DescribesStatus(StudioExecutionStatus status, string expected)
	{
		status.ToDisplayString().Should().Be(expected);
	}

	/// <summary>An undefined status falls back to its numeric name.</summary>
	[Fact]
	public void ToDisplayString_UndefinedStatus_FallsBack()
	{
		((StudioExecutionStatus)999).ToDisplayString().Should().Be("999");
	}
}
