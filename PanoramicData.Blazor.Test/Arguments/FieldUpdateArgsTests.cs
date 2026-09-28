using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="FieldUpdateArgs{TItem}"/>.</summary>
public class FieldUpdateArgsTests
{
	/// <summary>The constructor captures the field and both values.</summary>
	[Fact]
	public void Constructor_CapturesFieldAndValues()
	{
		var field = new FormField<Item> { Id = "name" };

		var args = new FieldUpdateArgs<Item>(field, "old", "new");

		args.Field.Should().BeSameAs(field);
		args.OldValue.Should().Be("old");
		args.NewValue.Should().Be("new");
	}

	/// <summary>A handler can replace the new value, while the old value stays fixed.</summary>
	[Fact]
	public void NewValue_CanBeReplaced()
	{
		var args = new FieldUpdateArgs<Item>(new FormField<Item>(), 1, 2)
		{
			NewValue = null
		};

		args.NewValue.Should().BeNull();
		args.OldValue.Should().Be(1);
	}

	private sealed class Item
	{
		public string Name { get; set; } = string.Empty;
	}
}
