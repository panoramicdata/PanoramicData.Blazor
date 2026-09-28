using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="TableEventArgs{TItem}"/> and the table event argument types derived from it.</summary>
public class TableEventArgsTests
{
	/// <summary>The base type captures the item.</summary>
	[Fact]
	public void TableEventArgs_CapturesItem()
	{
		var item = new Item();

		new TableEventArgs<Item>(item).Item.Should().BeSameAs(item);
	}

	/// <summary>The cancellable type starts uncancelled and can be cancelled.</summary>
	[Fact]
	public void TableCancelEventArgs_CanBeCancelled()
	{
		var args = new TableCancelEventArgs<Item>(new Item());
		args.Cancel.Should().BeFalse();

		args.Cancel = true;

		args.Cancel.Should().BeTrue();
	}

	/// <summary>The before-edit type carries a text selection range for the editor.</summary>
	[Fact]
	public void TableBeforeEditEventArgs_CarriesSelection()
	{
		var args = new TableBeforeEditEventArgs<Item>(new Item()) { SelectionStart = 2, SelectionEnd = 5 };

		args.SelectionStart.Should().Be(2);
		args.SelectionEnd.Should().Be(5);
		args.Cancel.Should().BeFalse();
	}

	/// <summary>The after-edit type describes its new values as a comma separated list, showing null values explicitly.</summary>
	[Fact]
	public void TableAfterEditEventArgs_ToString_ListsNewValues()
	{
		var args = new TableAfterEditEventArgs<Item>(new Item());
		args.NewValues["Name"] = "Bob";
		args.NewValues["Age"] = 42;
		args.NewValues["Notes"] = null;

		args.ToString().Should().Be("Name = Bob, Age = 42, Notes = (null)");
	}

	/// <summary>The after-edit type describes an edit with no changes as an empty string.</summary>
	[Fact]
	public void TableAfterEditEventArgs_ToString_EmptyWhenNoValues()
	{
		new TableAfterEditEventArgs<Item>(new Item()).ToString().Should().BeEmpty();
	}

	/// <summary>The committed type describes its new values the same way as the after-edit type.</summary>
	[Fact]
	public void TableAfterEditCommittedEventArgs_ToString_ListsNewValues()
	{
		var item = new Item();
		var args = new TableAfterEditCommittedEventArgs<Item>(item)
		{
			NewValues = new Dictionary<string, object?> { ["Name"] = "Ann", ["Notes"] = null }
		};

		args.Item.Should().BeSameAs(item);
		args.ToString().Should().Be("Name = Ann, Notes = (null)");
	}

	/// <summary>The committed type describes an edit with no changes as an empty string.</summary>
	[Fact]
	public void TableAfterEditCommittedEventArgs_ToString_EmptyWhenNoValues()
	{
		new TableAfterEditCommittedEventArgs<Item>(new Item()).ToString().Should().BeEmpty();
	}

	/// <summary>The selection type captures the selected items.</summary>
	[Fact]
	public void TableSelectionEventArgs_CapturesItems()
	{
		Item[] items = [new Item(), new Item()];

		new TableSelectionEventArgs<Item>(items).Items.Should().BeSameAs(items);
	}

	private sealed class Item
	{
		public string Name { get; set; } = string.Empty;
	}
}
