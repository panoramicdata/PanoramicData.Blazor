using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Enums;

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

	/// <summary>The old constructors derive the position from Before.</summary>
	[Theory]
	[InlineData(true, DropPosition.Before)]
	[InlineData(false, DropPosition.After)]
	[InlineData(null, DropPosition.Into)]
	public void BeforeConstructors_DeriveThePosition(bool? before, DropPosition expected)
	{
		new DropEventArgs("target", "payload", false, before).Position.Should().Be(expected);
		new DropEventArgs("target", "payload", false).Position.Should().Be(DropPosition.Into);
	}

	/// <summary>The position constructor sets Before to match, so existing consumers keep working.</summary>
	[Theory]
	[InlineData(DropPosition.Before, true)]
	[InlineData(DropPosition.After, false)]
	[InlineData(DropPosition.Into, null)]
	public void PositionConstructor_SetsBeforeToMatch(DropPosition position, bool? expected)
	{
		var args = new DropEventArgs("target", "payload", true, position);

		args.Position.Should().Be(position);
		args.Before.Should().Be(expected);
		args.Target.Should().Be("target");
		args.Payload.Should().Be("payload");
		args.Ctrl.Should().BeTrue();
	}
}