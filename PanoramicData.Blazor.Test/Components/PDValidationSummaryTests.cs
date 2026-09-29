using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDValidationSummary"/> lists each field error it is given, and nothing otherwise.
/// </summary>
public class PDValidationSummaryTests : BunitContext
{
	/// <summary>
	/// Verifies that every entry of a dictionary of errors becomes one row carrying the field and its message.
	/// </summary>
	[Fact]
	public void A_dictionary_of_errors_renders_one_row_per_field()
	{
		var errors = new Dictionary<string, string>
		{
			["Name"] = "Name is required",
			["Age"] = "Age must be positive"
		};

		var component = Render<PDValidationSummary>(parameters => parameters
			.Add(p => p.Errors, errors));

		var rows = component.FindAll("table.pd-validation-summary tr");
		rows.Should().HaveCount(2);
		component.FindAll("td.field").Select(td => td.TextContent).Should().Equal("Name", "Age");
		component.FindAll("td.error").Select(td => td.TextContent)
			.Should().Equal("Name is required", "Age must be positive");
	}

	/// <summary>
	/// Verifies that an Errors value that is not a string dictionary renders an empty table rather than
	/// attempting to interpret it.
	/// </summary>
	[Fact]
	public void Errors_that_are_not_a_string_dictionary_render_no_rows()
	{
		var component = Render<PDValidationSummary>(parameters => parameters
			.Add(p => p.Errors, new List<string> { "Name is required" }));

		component.Find("table.pd-validation-summary").Should().NotBeNull();
		component.FindAll("tr").Should().BeEmpty();
	}

	/// <summary>Verifies that no errors at all renders an empty table.</summary>
	[Fact]
	public void Null_errors_render_no_rows()
	{
		var component = Render<PDValidationSummary>();

		component.FindAll("tr").Should().BeEmpty();
	}
}
