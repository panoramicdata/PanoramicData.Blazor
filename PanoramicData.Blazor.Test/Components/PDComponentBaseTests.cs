using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using Bunit;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests the behaviour <see cref="PDComponentBase"/> gives every component that derives from it:
/// default identity and state, data annotation and FluentValidation error collection, and the
/// enable/disable methods.
/// </summary>
public class PDComponentBaseTests : BunitContext
{
	/// <summary>
	/// Verifies the defaults: enabled, visible, no tooltip, and a generated id of the documented form.
	/// </summary>
	[Fact]
	public void Has_documented_defaults_and_a_generated_id()
	{
		var component = Render<ProbeComponent>(parameters => parameters
			.Add(p => p.Name, "valid"));

		var probe = component.Instance;
		probe.IsEnabled.Should().BeTrue();
		probe.IsVisible.Should().BeTrue();
		probe.ToolTip.Should().BeEmpty();
		probe.CssClass.Should().BeNull();
		probe.Size.Should().BeNull();
		probe.Id.Should().MatchRegex("^pd-component-[0-9]+$");
	}

	/// <summary>
	/// Verifies that two instances are given different generated ids.
	/// </summary>
	[Fact]
	public void Generated_ids_are_unique_per_instance()
	{
		var first = Render<ProbeComponent>(parameters => parameters.Add(p => p.Name, "a"));
		var second = Render<ProbeComponent>(parameters => parameters.Add(p => p.Name, "b"));

		first.Instance.Id.Should().NotBe(second.Instance.Id);
	}

	/// <summary>
	/// Verifies that a valid component reports itself valid after its parameters are set.
	/// </summary>
	[Fact]
	public void A_component_satisfying_its_annotations_is_valid()
	{
		var component = Render<ProbeComponent>(parameters => parameters
			.Add(p => p.Name, "valid"));

		component.Instance.Valid.Should().BeTrue();
		component.Find(".errors").TextContent.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a data annotation failure is recorded against the member name with its message.
	/// </summary>
	[Fact]
	public void A_data_annotation_failure_is_recorded_by_member_name()
	{
		var component = Render<ProbeComponent>();

		component.Instance.Valid.Should().BeFalse();
		component.Instance.Errors.Should().ContainKey(nameof(ProbeComponent.Name))
			.WhoseValue.Should().Be("Name is required");
	}

	/// <summary>
	/// Verifies that errors from a previous parameter set are cleared once the component becomes valid.
	/// </summary>
	[Fact]
	public void Errors_are_cleared_when_parameters_become_valid()
	{
		var component = Render<ProbeComponent>();
		component.Instance.Valid.Should().BeFalse();

		component.Render(parameters => parameters.Add(p => p.Name, "now valid"));

		component.Instance.Valid.Should().BeTrue();
		component.Instance.Errors.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a result naming several members is keyed by the joined member names, and that a
	/// second result for the same key does not overwrite the first.
	/// </summary>
	[Fact]
	public void Annotation_results_are_keyed_by_joined_member_names_and_first_wins()
	{
		var component = Render<ProbeComponent>(parameters => parameters.Add(p => p.Name, "valid"));

		component.Instance.ApplyAnnotationResults(
			new ValidationResult("first", ["A", "B"]),
			new ValidationResult("second", ["A", "B"]),
			new ValidationResult(null, ["C"]));

		component.Instance.Errors.Should().HaveCount(2);
		component.Instance.Errors["A, B"].Should().Be("first");
		component.Instance.Errors["C"].Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that FluentValidation failures are added by property name, and that when several rules
	/// fail for one property the first failure's message is kept.
	/// </summary>
	[Fact]
	public void FluentValidation_failures_are_recorded_by_property_name()
	{
		var component = Render<ProbeComponent>(parameters => parameters.Add(p => p.Name, "valid"));
		var validator = new InlineValidator<Candidate>();
		validator.RuleFor(x => x.Age).GreaterThan(0).WithMessage("Age must be positive");
		validator.RuleFor(x => x.Age).NotEqual(-1).WithMessage("Age must not be minus one");

		component.Instance.RunFluentValidation(validator, new Candidate { Age = -1 });

		component.Instance.Valid.Should().BeFalse();
		component.Instance.Errors.Should().ContainSingle()
			.Which.Should().Be(new KeyValuePair<string, string>(nameof(Candidate.Age), "Age must be positive"));
	}

	/// <summary>
	/// Verifies that a FluentValidation pass leaves the error collection untouched.
	/// </summary>
	[Fact]
	public void A_passing_FluentValidation_adds_no_errors()
	{
		var component = Render<ProbeComponent>(parameters => parameters.Add(p => p.Name, "valid"));
		var validator = new InlineValidator<Candidate>();
		validator.RuleFor(x => x.Age).GreaterThanOrEqualTo(0);

		component.Instance.RunFluentValidation(validator, new Candidate { Age = 1 });

		component.Instance.Valid.Should().BeTrue();
	}

	/// <summary>
	/// Verifies that Disable, Enable and SetEnabled change the state and re-render the component.
	/// </summary>
	[Fact]
	public async Task Enable_disable_and_SetEnabled_change_state_and_rerender()
	{
		var component = Render<ProbeComponent>(parameters => parameters.Add(p => p.Name, "valid"));
		component.Find(".state").TextContent.Should().Be("enabled");

		await component.InvokeAsync(component.Instance.Disable);
		component.Find(".state").TextContent.Should().Be("disabled");

		await component.InvokeAsync(component.Instance.Enable);
		component.Find(".state").TextContent.Should().Be("enabled");

		await component.InvokeAsync(() => component.Instance.SetEnabled(false));
		component.Instance.IsEnabled.Should().BeFalse();
		component.Find(".state").TextContent.Should().Be("disabled");

		await component.InvokeAsync(() => component.Instance.SetEnabled(true));
		component.Find(".state").TextContent.Should().Be("enabled");
	}

	/// <summary>
	/// A model validated by FluentValidation in these tests.
	/// </summary>
	private sealed class Candidate
	{
		/// <summary>Gets or sets the age being validated.</summary>
		public int Age { get; set; }
	}

	/// <summary>
	/// A minimal component deriving from <see cref="PDComponentBase"/> that exposes its protected members.
	/// </summary>
	private sealed class ProbeComponent : PDComponentBase
	{
		/// <summary>Gets or sets a required value, validated by data annotations.</summary>
		[Parameter]
		[Required(ErrorMessage = "Name is required")]
		public string? Name { get; set; }

		/// <summary>Gets the validation errors.</summary>
		public Dictionary<string, string> Errors => ValidationErrors;

		/// <summary>Gets whether the component is valid.</summary>
		public bool Valid => IsValid;

		/// <summary>Merges the given data annotation results.</summary>
		/// <param name="results">The results to merge.</param>
		public void ApplyAnnotationResults(params ValidationResult[] results) => SetValidationErrors(results);

		/// <summary>Runs the given FluentValidation validator.</summary>
		/// <param name="validator">The validator.</param>
		/// <param name="candidate">The object to validate.</param>
		public void RunFluentValidation(IValidator<Candidate> validator, Candidate candidate) => FluentValidate(validator, candidate);

		/// <inheritdoc />
		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenElement(0, "span");
			builder.AddAttribute(1, "class", "state");
			builder.AddContent(2, IsEnabled ? "enabled" : "disabled");
			builder.CloseElement();
			builder.OpenElement(3, "span");
			builder.AddAttribute(4, "class", "errors");
			builder.AddContent(5, string.Join(";", ValidationErrors.Values));
			builder.CloseElement();
		}
	}
}
