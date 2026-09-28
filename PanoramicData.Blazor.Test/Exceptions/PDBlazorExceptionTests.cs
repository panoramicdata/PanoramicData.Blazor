using AwesomeAssertions;
using PanoramicData.Blazor.Exceptions;

namespace PanoramicData.Blazor.Test.Exceptions;

/// <summary>Tests for <see cref="PDBlazorException"/>.</summary>
public class PDBlazorExceptionTests
{
	/// <summary>The parameterless constructor produces an exception with no inner exception.</summary>
	[Fact]
	public void DefaultConstructor_HasNoInnerException()
	{
		var exception = new PDBlazorException();

		exception.InnerException.Should().BeNull();
		exception.Message.Should().NotBeNullOrEmpty();
	}

	/// <summary>The message constructor keeps the message.</summary>
	[Fact]
	public void MessageConstructor_KeepsMessage()
	{
		var exception = new PDBlazorException("Something failed");

		exception.Message.Should().Be("Something failed");
		exception.InnerException.Should().BeNull();
	}

	/// <summary>The message and inner exception constructor keeps both.</summary>
	[Fact]
	public void MessageAndInnerConstructor_KeepsBoth()
	{
		var inner = new InvalidOperationException("inner");

		var exception = new PDBlazorException("outer", inner);

		exception.Message.Should().Be("outer");
		exception.InnerException.Should().BeSameAs(inner);
	}

	/// <summary>The exception can be caught as a <see cref="PDBlazorException"/>.</summary>
	[Fact]
	public void Throwing_CanBeCaughtAsLibraryException()
	{
		Action act = () => throw new PDBlazorException("boom");

		act.Should().Throw<PDBlazorException>().WithMessage("boom");
	}
}
