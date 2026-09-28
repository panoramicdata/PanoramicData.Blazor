using AwesomeAssertions;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Arguments;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="KeyedEventArgs{T}"/>.</summary>
public class KeyedEventArgsTests
{
	/// <summary>The two-argument constructor keeps the supplied arguments instance.</summary>
	[Fact]
	public void ConstructorWithArgs_KeepsArgs()
	{
		var mouse = new MouseEventArgs { ClientX = 5 };

		var args = new KeyedEventArgs<MouseEventArgs>("save", mouse);

		args.Key.Should().Be("save");
		args.Args.Should().BeSameAs(mouse);
	}

	/// <summary>The key-only constructor creates a default arguments instance rather than leaving it null.</summary>
	[Fact]
	public void ConstructorWithKeyOnly_CreatesDefaultArgs()
	{
		var args = new KeyedEventArgs<MouseEventArgs>("open");

		args.Key.Should().Be("open");
		args.Args.Should().NotBeNull();
		args.Args.ClientX.Should().Be(0);
	}
}
