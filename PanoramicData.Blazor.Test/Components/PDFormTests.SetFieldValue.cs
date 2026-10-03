using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDForm{TItem}"/> records field value edits as a delta.
/// </summary>
public partial class PDFormTests
{
	/// <summary>
	/// Verifies that an edit is recorded as a delta, announced with its old and new values, pushed to the
	/// field, and arms the unload guard.
	/// </summary>
	[Fact]
	public async Task SetFieldValueAsync_RecordsTheDelta_AndAnnouncesIt()
	{
		var updates = new List<FieldUpdateArgs<Person>>();
		var form = RenderForm(p => p.Add(x => x.FieldUpdated, args => updates.Add(args)));
		var field = AddField(form, p => p.Name);
		var person = new Person { Name = "Ann" };
		await EditAsync(form, person, FormModes.Edit);
		object? notified = null;
		field.ValueChanged += (_, value) => notified = value;

		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob"));
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Cy"));

		form.Instance.Delta.Should().ContainKey("Name").WhoseValue.Should().Be("Cy");
		form.Instance.HasChanges.Should().BeTrue();
		person.Name.Should().Be("Ann");
		updates.Select(u => (u.OldValue, u.NewValue)).Should().Equal(("Ann", "Bob"), ("Bob", "Cy"));
		notified.Should().Be("Cy");
		_module.Invocations["setUnloadListener"][^1].Arguments.Should().Equal(form.Instance.Id, true);
	}

	/// <summary>
	/// Verifies that setting a field back to its original value removes it from the delta and disarms the
	/// unload guard.
	/// </summary>
	[Fact]
	public async Task SetFieldValueAsync_RevertingToTheOriginal_RemovesTheDelta()
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Name);
		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit);
		var disarmedBefore = UnloadListenerCalls(false);

		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob"));
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Ann"));

		form.Instance.Delta.Should().BeEmpty();
		form.Instance.HasChanges.Should().BeFalse();
		UnloadListenerCalls(false).Should().Be(disarmedBefore + 1);
	}

	/// <summary>
	/// Verifies that an unchanged value, including one that differs only in line endings, is not an edit.
	/// </summary>
	[Theory]
	[InlineData("first\r\nsecond", "first\nsecond")]
	[InlineData("same", "same")]
	public async Task SetFieldValueAsync_WithAnEquivalentValue_IsNotAnEdit(string original, string entered)
	{
		var updates = 0;
		var form = RenderForm(p => p.Add(x => x.FieldUpdated, _ => updates++));
		var field = AddField(form, p => p.Notes);
		await EditAsync(form, new Person { Notes = original }, FormModes.Edit);

		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, entered));

		form.Instance.Delta.Should().BeEmpty();
		updates.Should().Be(0);
	}

	/// <summary>
	/// Verifies that a value of another type is converted to the property type before it is recorded.
	/// </summary>
	[Fact]
	public async Task SetFieldValueAsync_ConvertsTheValueToThePropertyType()
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Age);
		await EditAsync(form, new Person { Name = "Ann", Age = 30 }, FormModes.Edit);

		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, 42L));

		form.Instance.Delta["Age"].Should().Be(42);
		form.Instance.GetFieldValue<int>("Age").Should().Be(42);
		form.Instance.GetFieldValue<int>("Age", false).Should().Be(30);
	}

	/// <summary>
	/// Verifies that with automatic delta application an edit in edit mode is written straight to the item,
	/// and that suppressing field notification does not tell the field.
	/// </summary>
	[Fact]
	public async Task SetFieldValueAsync_WithAutoApplyDelta_UpdatesTheItem()
	{
		var form = RenderForm(p => p.Add(x => x.AutoApplyDelta, true));
		var field = AddField(form, p => p.Name);
		var person = new Person { Name = "Ann" };
		await EditAsync(form, person, FormModes.Edit);
		var notified = false;
		field.ValueChanged += (_, _) => notified = true;

		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob", notifyField: false));

		person.Name.Should().Be("Bob");
		notified.Should().BeFalse();
	}

	/// <summary>
	/// Verifies that with no item, or a field with no binding, a value is ignored.
	/// </summary>
	[Fact]
	public async Task SetFieldValueAsync_WithoutAnItemOrBinding_IsIgnored()
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Name);
		var unbound = new FormField<Person>();

		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob"));
		await EditAsync(form, new Person(), FormModes.Edit);
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(unbound, "Bob"));

		form.Instance.Delta.Should().BeEmpty();
	}
}
