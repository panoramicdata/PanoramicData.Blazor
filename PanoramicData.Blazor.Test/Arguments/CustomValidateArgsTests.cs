using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="CustomValidateArgs{TItem}"/>.</summary>
public class CustomValidateArgsTests
{
	/// <summary>The constructor captures the field and item, and both message collections start empty.</summary>
	[Fact]
	public void Constructor_CapturesFieldAndItem()
	{
		var field = new FormField<Item> { Title = "Name" };
		var item = new Item();

		var args = new CustomValidateArgs<Item>(field, item);

		args.Field.Should().BeSameAs(field);
		args.Item.Should().BeSameAs(item);
		args.AddErrorMessages.Should().BeEmpty();
		args.RemoveErrorMessages.Should().BeEmpty();
	}

	/// <summary>Error messages can be queued for addition and removal independently.</summary>
	[Fact]
	public void MessageCollections_AreIndependent()
	{
		var args = new CustomValidateArgs<Item>(new FormField<Item>(), null);

		args.AddErrorMessages["Name"] = "Required";
		args.RemoveErrorMessages["Age"] = "Too young";

		args.Item.Should().BeNull();
		args.AddErrorMessages.Should().ContainSingle().Which.Key.Should().Be("Name");
		args.RemoveErrorMessages.Should().ContainSingle().Which.Value.Should().Be("Too young");
	}

	private sealed class Item
	{
		public string Name { get; set; } = string.Empty;
	}
}
