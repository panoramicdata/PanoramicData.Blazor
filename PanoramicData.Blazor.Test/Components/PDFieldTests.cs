using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDField{TItem}"/>: how it resolves its title and how it registers itself with its form.
/// </summary>
public class PDFieldTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDFieldTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDForm<Person>> RenderForm(Action<ComponentParameterCollectionBuilder<PDField<Person>>> field)
		=> Render<PDForm<Person>>(form => form
			.Add(p => p.Item, new Person())
			.AddChildContent<PDFormBody<Person>>(body => body
				.AddChildContent<PDField<Person>>(field)));

	/// <summary>
	/// Verifies that a field inside a form body registers itself, carrying its settings into the form's field list.
	/// </summary>
	[Fact]
	public void Field_RegistersItselfWithTheForm()
	{
		var form = RenderForm(field => field
			.Add(p => p.Id, "name")
			.Add(p => p.Field, person => person.Name)
			.Add(p => p.Group, "General")
			.Add(p => p.MaxLength, 20)
			.Add(p => p.IsTextArea, true));

		var registered = form.Instance.Fields.Should().ContainSingle().Subject;
		registered.Id.Should().Be("name");
		registered.Group.Should().Be("General");
		registered.MaxLength.Should().Be(20);
		registered.IsTextArea.Should().BeTrue();
	}

	/// <summary>
	/// Verifies that the title falls back to the property's display name.
	/// </summary>
	[Fact]
	public void Title_DefaultsToTheDisplayAttributeName()
	{
		var form = RenderForm(field => field.Add(p => p.Field, person => person.Name));

		form.Instance.Fields.Single().Title.Should().Be("Full name");
	}

	/// <summary>
	/// Verifies that a property with no display attribute takes its title from the property name.
	/// </summary>
	[Fact]
	public void Title_WithoutADisplayAttribute_IsThePropertyName()
	{
		var form = RenderForm(field => field.Add(p => p.Field, person => person.Age));

		form.Instance.Fields.Single().Title.Should().Be(nameof(Person.Age));
	}

	/// <summary>
	/// Verifies that an explicit title overrides the display name.
	/// </summary>
	[Fact]
	public void Title_Parameter_OverridesTheDisplayName()
	{
		var form = RenderForm(field => field
			.Add(p => p.Field, person => person.Name)
			.Add(p => p.Title, "Override"));

		form.Instance.Fields.Single().Title.Should().Be("Override");
	}

	/// <summary>
	/// Verifies that a title function takes precedence over an explicit title and is given the item.
	/// </summary>
	[Fact]
	public void GetTitle_PrefersTheTitleFunction()
	{
		var form = RenderForm(field => field
			.Add(p => p.Field, person => person.Name)
			.Add(p => p.Title, "Override")
			.Add(p => p.TitleFunc, person => $"Name of {person?.Age}"));
		var field = form.FindComponent<PDField<Person>>().Instance;

		field.GetTitle(new Person { Age = 7 }).Should().Be("Name of 7");
	}

	/// <summary>
	/// Verifies that a field bound to a public field member, rather than a property, is titled by the member name.
	/// </summary>
	[Fact]
	public void GetTitle_ForAFieldMember_IsTheMemberName()
	{
		var form = RenderForm(field => field.Add(p => p.Field, (Expression<Func<Person, object>>)(person => person.Nickname)));

		form.FindComponent<PDField<Person>>().Instance.GetTitle().Should().Be(nameof(Person.Nickname));
	}

	/// <summary>
	/// Verifies that a field with nothing to take a title from has an empty title.
	/// </summary>
	[Fact]
	public void GetTitle_WithNoFieldOrTitle_IsEmpty()
	{
		var form = RenderForm(_ => { });

		form.FindComponent<PDField<Person>>().Instance.GetTitle().Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a field rendered outside a form body fails with an explanation rather than silently doing nothing.
	/// </summary>
	[Fact]
	public void Field_OutsideAFormBody_Throws()
	{
		var act = () => Render<PDField<Person>>(field => field.Add(p => p.Field, person => person.Name));

		act.Should().Throw<InvalidOperationException>().WithMessage("*FormBody reference is null*");
	}

	private sealed class Person
	{
		[Display(Name = "Full name")]
		public string Name { get; set; } = string.Empty;

		public int Age { get; set; }

		public string Nickname = string.Empty;
	}
}
