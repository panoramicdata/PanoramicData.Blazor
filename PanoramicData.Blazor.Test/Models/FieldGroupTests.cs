using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="FieldGroup{TItem}"/>.</summary>
public class FieldGroupTests
{
	/// <summary>A new group has no id, fields or title.</summary>
	[Fact]
	public void New_IsEmpty()
	{
		var group = new FieldGroup<Item>();

		group.Id.Should().BeEmpty();
		group.Fields.Should().BeEmpty();
		group.Title.Should().BeEmpty();
		group.GetTitle().Should().BeEmpty();
	}

	/// <summary>The title is the first field's group name when it has one.</summary>
	[Fact]
	public void Title_UsesFirstFieldGroupName()
	{
		var group = new FieldGroup<Item>
		{
			Fields = [new FormField<Item> { Group = "Address", Title = "Street" }, new FormField<Item> { Group = "Other" }]
		};

		group.Title.Should().Be("Address");
		group.GetTitle(new Item()).Should().Be("Address");
	}

	/// <summary>Without a group name, the title falls back to the first field's own title.</summary>
	[Fact]
	public void Title_FallsBackToFirstFieldTitle()
	{
		var group = new FieldGroup<Item> { Fields = [new FormField<Item> { Group = " ", Title = "Name" }] };

		group.Title.Should().Be("Name");
		group.GetTitle().Should().Be("Name");
	}

	/// <summary>GetTitle evaluates the first field's title function against the item.</summary>
	[Fact]
	public void GetTitle_UsesFieldTitleFunction()
	{
		var group = new FieldGroup<Item>
		{
			Fields = [new FormField<Item> { Title = "Static", TitleFunc = item => $"Name of {item?.Name}" }]
		};

		group.GetTitle(new Item { Name = "Bob" }).Should().Be("Name of Bob");
		group.Title.Should().Be("Static");
	}

	/// <summary>Without a form there can be no errors.</summary>
	[Fact]
	public void HasErrors_NullForm_IsFalse()
	{
		var group = new FieldGroup<Item> { Fields = [new FormField<Item> { Field = x => x.Name }] };

		group.HasErrors(null).Should().BeFalse();
	}

	/// <summary>The group has errors only when the form has an error recorded against one of its fields.</summary>
	[Fact]
	public void HasErrors_ReflectsFormErrorsForGroupFields()
	{
		var group = new FieldGroup<Item> { Fields = [new FormField<Item> { Field = x => x.Name }] };
		var form = new PDForm<Item>();

		group.HasErrors(form).Should().BeFalse();

		form.Errors["Age"] = ["Too young"];
		group.HasErrors(form).Should().BeFalse();

		form.Errors["Name"] = ["Required"];
		group.HasErrors(form).Should().BeTrue();
	}

	private sealed class Item
	{
		public string Name { get; set; } = string.Empty;

		public int Age { get; set; }
	}
}
