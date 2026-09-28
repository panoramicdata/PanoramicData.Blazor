using AwesomeAssertions;
using PanoramicData.Blazor.Exceptions;

namespace PanoramicData.Blazor.Test.Exceptions;

/// <summary>Tests for <see cref="StateException"/>.</summary>
public class StateExceptionTests
{
	/// <summary>The message constructor keeps the message and has no inner exception.</summary>
	[Fact]
	public void MessageConstructor_KeepsMessage()
	{
		var exception = new StateException("Bad state");

		exception.Message.Should().Be("Bad state");
		exception.InnerException.Should().BeNull();
	}

	/// <summary>The message and inner exception constructor keeps both.</summary>
	[Fact]
	public void MessageAndInnerConstructor_KeepsBoth()
	{
		var inner = new FormatException("inner");

		var exception = new StateException("outer", inner);

		exception.Message.Should().Be("outer");
		exception.InnerException.Should().BeSameAs(inner);
	}

	/// <summary>A null message and inner exception are accepted.</summary>
	[Fact]
	public void NullArguments_AreAccepted()
	{
		var exception = new StateException(null, null);

		exception.InnerException.Should().BeNull();
		exception.Message.Should().NotBeNull();
	}
}
