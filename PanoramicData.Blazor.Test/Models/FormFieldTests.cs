using AwesomeAssertions;
using PanoramicData.Blazor.Models;
using System.ComponentModel.DataAnnotations;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="FormField{TItem}"/>.</summary>
public class FormFieldTests
{
	/// <summary>A new field is visible in edit and create, hidden in delete, editable, and has no binding.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var field = new FormField<Item>();

		field.ShowInEdit(null).Should().BeTrue();
		field.ShowInCreate(null).Should().BeTrue();
		field.ShowInDelete(null).Should().BeFalse();
		field.ReadOnlyInEdit(null).Should().BeFalse();
		field.ReadOnlyInCreate(null).Should().BeFalse();
		field.ShowCopyButton(null).Should().BeFalse();
		field.IsSensitive(null, null).Should().BeFalse();
		field.TextAreaRows.Should().Be(4);
		field.ShowValidationResult.Should().BeTrue();
		field.Name.Should().BeEmpty();
		field.GetName().Should().BeNull();
		field.GetFieldType().Should().BeNull();
		field.GetFieldIsNullable().Should().BeFalse();
		field.GetIsRequired().Should().BeFalse();
		field.CompiledFieldFunc.Should().BeNull();
	}

	/// <summary>The value changed event is raised with the field as sender and the new value.</summary>
	[Fact]
	public void OnValueChanged_RaisesEvent()
	{
		var field = new FormField<Item>();
		object? sender = null;
		object? value = null;
		field.ValueChanged += (s, v) => { sender = s; value = v; };

		field.OnValueChanged("new");

		sender.Should().BeSameAs(field);
		value.Should().Be("new");
	}

	/// <summary>Raising the value changed event with no subscribers does nothing.</summary>
	[Fact]
	public void OnValueChanged_NoSubscribers_DoesNotThrow()
	{
		var act = () => new FormField<Item>().OnValueChanged(1);

		act.Should().NotThrow();
	}

	/// <summary>The title function takes precedence over the title and receives the item.</summary>
	[Fact]
	public void GetTitle_PrefersTitleFunction()
	{
		var field = new FormField<Item> { Title = "Plain" };
		field.GetTitle().Should().Be("Plain");

		field.TitleFunc = item => item is null ? "No item" : $"Name ({item.Name})";

		field.GetTitle().Should().Be("No item");
		field.GetTitle(new Item { Name = "Ann" }).Should().Be("Name (Ann)");
	}

	/// <summary>The name is taken from the bound property, including through a boxing conversion.</summary>
	[Fact]
	public void Name_ComesFromBoundProperty()
	{
		new FormField<Item> { Field = x => x.Name }.Name.Should().Be("Name");
		new FormField<Item> { Field = x => x.Age }.Name.Should().Be("Age");
		new FormField<Item> { Field = x => x.Age }.GetName().Should().Be("Age");
	}

	/// <summary>The render value of a missing item is null, and of an ordinary value is the value itself.</summary>
	[Fact]
	public void GetRenderValue_ReturnsNullForNullItemAndRawValueOtherwise()
	{
		var field = new FormField<Item> { Field = x => x.Age };

		field.GetRenderValue(null).Should().BeNull();
		field.GetRenderValue(new Item { Age = 7 }).Should().Be(7);
	}

	/// <summary>Date and date/time offset values render as ISO dates.</summary>
	[Fact]
	public void GetRenderValue_FormatsDates()
	{
		var item = new Item
		{
			Born = new DateTime(2020, 2, 3, 4, 5, 6, DateTimeKind.Utc),
			Seen = new DateTimeOffset(2021, 12, 31, 23, 0, 0, TimeSpan.FromHours(2))
		};

		new FormField<Item> { Field = x => x.Born }.GetRenderValue(item).Should().Be("2020-02-03");
		new FormField<Item> { Field = x => x.Seen }.GetRenderValue(item).Should().Be("2021-12-31");
	}

	/// <summary>A null value is passed through as null.</summary>
	[Fact]
	public void GetRenderValue_NullValue_IsNull()
	{
		new FormField<Item> { Field = x => x.Notes! }.GetRenderValue(new Item()).Should().BeNull();
	}

	/// <summary>The field type is the bound property's type, with nullable wrappers removed.</summary>
	[Fact]
	public void GetFieldType_UnwrapsNullable()
	{
		new FormField<Item> { Field = x => x.Name }.GetFieldType().Should().Be<string>();
		new FormField<Item> { Field = x => x.Age }.GetFieldType().Should().Be<int>();
		new FormField<Item> { Field = x => x.Score! }.GetFieldType().Should().Be<int>();
		new FormField<Item> { Field = x => x.Age + 1 }.GetFieldType().Should().BeNull();
	}

	/// <summary>Strings and nullable value types accept null; plain value types do not.</summary>
	[Fact]
	public void GetFieldIsNullable_ReflectsPropertyType()
	{
		new FormField<Item> { Field = x => x.Name }.GetFieldIsNullable().Should().BeTrue();
		new FormField<Item> { Field = x => x.Score! }.GetFieldIsNullable().Should().BeTrue();
		new FormField<Item> { Field = x => x.Age }.GetFieldIsNullable().Should().BeFalse();
	}

	/// <summary>A property marked Required is reported as required.</summary>
	[Fact]
	public void GetIsRequired_ReflectsRequiredAttribute()
	{
		new FormField<Item> { Field = x => x.Name }.GetIsRequired().Should().BeTrue();
		new FormField<Item> { Field = x => x.Age }.GetIsRequired().Should().BeFalse();
	}

	/// <summary>The description function returns the explicit description, else the Display attribute description.</summary>
	[Fact]
	public void DescriptionFunc_PrefersExplicitDescription()
	{
		var field = new FormField<Item> { Field = x => x.Name };
		field.DescriptionFunc(field, null).Should().Be("The person's name");

		field.Description = "Explicit";

		field.DescriptionFunc(field, null).Should().Be("Explicit");
	}

	/// <summary>The obsolete static helpers still return the constant true and false functions.</summary>
	[Fact]
	public void ObsoleteStaticHelpers_ReturnConstants()
	{
#pragma warning disable CS0618 // Exercising the members kept for backward compatibility.
		FormField<Item>.True(null).Should().BeTrue();
		FormField<Item>.False(null).Should().BeFalse();
#pragma warning restore CS0618
	}

	/// <summary>The remaining descriptive members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var helper = new FormFieldHelper<Item>();
		var field = new FormField<Item>
		{
			AutoComplete = "off",
			DisplayOptions = new FieldDisplayOptions { WidthWeight = 2 },
			Group = "G",
			Id = "id1",
			Label = "L",
			SuppressErrors = true,
			IsPassword = true,
			IsTextArea = true,
			TextAreaRows = 8,
			IsImage = true,
			Helper = helper,
			HelpUrl = "https://example.com/help",
			MaxLength = 10,
			MaxValue = 5,
			MinValue = 1,
			ShowValidationResult = false,
			Options = (_, _) => [new OptionInfo { Text = "A" }],
			OptionsAsync = (_, _) => Task.FromResult<OptionInfo[]>([])
		};

		field.AutoComplete.Should().Be("off");
		field.DisplayOptions!.WidthWeight.Should().Be(2);
		field.Group.Should().Be("G");
		field.Id.Should().Be("id1");
		field.Label.Should().Be("L");
		field.SuppressErrors.Should().BeTrue();
		field.IsPassword.Should().BeTrue();
		field.IsTextArea.Should().BeTrue();
		field.TextAreaRows.Should().Be(8);
		field.IsImage.Should().BeTrue();
		field.Helper.Should().BeSameAs(helper);
		field.HelpUrl.Should().Be("https://example.com/help");
		field.MaxLength.Should().Be(10);
		field.MaxValue.Should().Be(5);
		field.MinValue.Should().Be(1);
		field.ShowValidationResult.Should().BeFalse();
		field.Options!(field, null).Should().ContainSingle();
		field.EditTemplate.Should().BeNull();
	}

	private sealed class Item
	{
		[Required]
		[Display(Description = "The person's name")]
		public string Name { get; set; } = string.Empty;

		public int Age { get; set; }

		public int? Score { get; set; }

		public string? Notes { get; set; }

		public DateTime Born { get; set; }

		public DateTimeOffset Seen { get; set; }
	}
}
