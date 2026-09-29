using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="DropEventArgs"/>.</summary>
public class DropEventArgsTests
{
	/// <summary>The three-argument constructor leaves the before/after position unknown.</summary>
	[Fact]
	public void ThreeArgumentConstructor_LeavesBeforeNull()
	{
		var target = new object();
		var payload = new object();

		var args = new DropEventArgs(target, payload, true);

		args.Target.Should().BeSameAs(target);
		args.Payload.Should().BeSameAs(payload);
		args.Ctrl.Should().BeTrue();
		args.Before.Should().BeNull();
	}

	/// <summary>The four-argument constructor records whether the drop was before or after the target.</summary>
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void FourArgumentConstructor_RecordsBefore(bool before)
	{
		var args = new DropEventArgs("target", "payload", false, before);

		args.Target.Should().Be("target");
		args.Payload.Should().Be("payload");
		args.Ctrl.Should().BeFalse();
		args.Before.Should().Be(before);
	}

	/// <summary>The target can be retargeted after construction.</summary>
	[Fact]
	public void Target_IsSettable()
	{
		var args = new DropEventArgs(null, null, false)
		{
			Target = "new"
		};

		args.Target.Should().Be("new");
		args.Payload.Should().BeNull();
	}
}
