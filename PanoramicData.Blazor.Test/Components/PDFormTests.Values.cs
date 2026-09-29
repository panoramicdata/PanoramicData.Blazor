using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Field value reading tests for <see cref="PDForm{TItem}"/>.
/// </summary>
public partial class PDFormTests
{
	/// <summary>
	/// Verifies that field values are read from the delta when asked for the updated value, and from the item
	/// when asked for the original.
	/// </summary>
	[Fact]
	public async Task GetFieldValue_ReadsTheUpdatedOrOriginalValue()
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Name);
		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit);
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob"));

		form.Instance.GetFieldValue("Name").Should().Be("Bob");
		form.Instance.GetFieldValue("Name", false).Should().Be("Ann");
		form.Instance.GetFieldValue(field).Should().Be("Bob");
		form.Instance.GetFieldStringValue("Name").Should().Be("Bob");
		form.Instance.GetFieldStringValue("Name", false).Should().Be("Ann");
		form.Instance.GetFieldStringValue(field).Should().Be("Bob");
	}

	/// <summary>
	/// Verifies that a field bound to a computed expression rather than a property is evaluated against the item.
	/// </summary>
	[Fact]
	public async Task GetFieldValue_OfAComputedField_EvaluatesTheExpression()
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Age * 2);
		await EditAsync(form, new Person { Name = "Ann", Age = 21 }, FormModes.Edit, validate: false);

		form.Instance.GetFieldValue(field).Should().Be(42);
		form.Instance.GetFieldValue<int>(field).Should().Be(42);
		form.Instance.GetFieldStringValue(field).Should().Be("42");
	}

	/// <summary>
	/// Verifies that typed reads convert compatible values and fall back to the default for incompatible or
	/// missing ones.
	/// </summary>
	[Fact]
	public async Task GetFieldValueOfT_ConvertsOrFallsBackToDefault()
	{
		var form = RenderForm();
		var name = AddField(form, p => p.Name);
		var age = AddField(form, p => p.Age);

		form.Instance.GetFieldValue<int>(age).Should().Be(0, "there is no item yet");
		await EditAsync(form, new Person { Name = "12", Age = 7 }, FormModes.Edit);

		form.Instance.GetFieldValue<int>(age).Should().Be(7);
		form.Instance.GetFieldValue<long>("Age").Should().Be(7L);
		form.Instance.GetFieldValue<int>(name).Should().Be(12);
		form.Instance.Item!.Name = "twelve";
		form.Instance.GetFieldValue<int>(name).Should().Be(0);
		form.Instance.GetFieldValue<int>((FormField<Person>)null!, true).Should().Be(0);
	}

	/// <summary>
	/// Verifies that string reads format dates as ISO dates, render null as empty and join several fields with tabs.
	/// </summary>
	[Fact]
	public async Task GetFieldStringValue_FormatsDates_AndJoinsFields()
	{
		var form = RenderForm();
		var born = AddField(form, p => p.Born);
		var joined = AddField(form, p => p.Joined!);
		var nickname = AddField(form, p => p.Nickname!);
		var person = new Person
		{
			Name = "Ann",
			Born = new DateTime(1990, 5, 17, 13, 45, 0, DateTimeKind.Unspecified),
			Joined = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero)
		};

		form.Instance.GetFieldStringValue(born).Should().BeEmpty("there is no item yet");
		form.Instance.GetFieldValue(born).Should().BeNull();
		await EditAsync(form, person, FormModes.Edit, validate: false);

		form.Instance.GetFieldStringValue(born).Should().Be("1990-05-17");
		form.Instance.GetFieldStringValue(joined).Should().Be("2020-01-02");
		form.Instance.GetFieldStringValue(nickname).Should().BeEmpty();
		form.Instance.GetFieldStringValue([born, nickname, joined]).Should().Be("1990-05-17\t\t2020-01-02");
		form.Instance.GetFieldStringValue([born], false).Should().Be("1990-05-17");
	}

	/// <summary>
	/// Verifies that asking for a field the form does not have returns null, as documented, and that the
	/// name-based value reads then return an empty or default value rather than throwing (#182).
	/// </summary>
	[Fact]
	public async Task GetField_ForAnUnknownName_ReturnsNull()
	{
		var form = RenderForm();
		AddField(form, p => p.Name);
		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit);

		form.Instance.GetField("Nope").Should().BeNull();
		form.Instance.GetFieldValue("Nope").Should().BeNull();
		form.Instance.GetFieldValue<int>("Nope").Should().Be(0);
		form.Instance.GetFieldStringValue("Nope").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the unload guard is armed once, on the first change, and not again on every later edit
	/// while one field is dirty (#182).
	/// </summary>
	[Fact]
	public async Task SetFieldValueAsync_ArmsTheUnloadGuardOnlyOnTheFirstChange()
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Name);
		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit);
		var armedBefore = UnloadListenerCalls(true);

		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob"));
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Cy"));
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Di"));

		UnloadListenerCalls(true).Should().Be(armedBefore + 1);
	}

	/// <summary>
	/// Verifies that a clone with the edits applied is returned without changing the item, and none without an item.
	/// </summary>
	[Fact]
	public async Task GetItemWithUpdates_ReturnsAnUpdatedClone()
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Name);
		form.Instance.GetItemWithUpdates().Should().BeNull();
		var person = new Person { Name = "Ann", Age = 3 };
		await EditAsync(form, person, FormModes.Edit);
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob"));

		var clone = form.Instance.GetItemWithUpdates();

		clone.Should().NotBeNull().And.NotBeSameAs(person);
		clone!.Name.Should().Be("Bob");
		clone.Age.Should().Be(3);
		person.Name.Should().Be("Ann");
	}
}
