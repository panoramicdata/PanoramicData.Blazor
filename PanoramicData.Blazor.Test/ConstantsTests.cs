using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests for <see cref="Constants"/> and the standard helper functions in <see cref="Constants.Functions"/>.
/// </summary>
public class ConstantsTests
{
	/// <summary>The select-all and select-none tokens have their documented values.</summary>
	[Fact]
	public void Tokens_HaveTheirDocumentedValues()
	{
		Constants.TokenAll.Should().Be("(All)");
		Constants.TokenNone.Should().Be("(None)");
	}

	/// <summary>True and False return their constant result whatever the argument.</summary>
	[Fact]
	public void TrueAndFalse_ReturnConstants()
	{
		Constants.Functions.True<string?>(null).Should().BeTrue();
		Constants.Functions.True(42).Should().BeTrue();
		Constants.Functions.False<string?>(null).Should().BeFalse();
		Constants.Functions.False(42).Should().BeFalse();
	}

	/// <summary>No item is sensitive by default.</summary>
	[Fact]
	public void FormFieldIsSensitive_IsAlwaysFalse()
	{
		Constants.Functions.FormFieldIsSensitive(new Item(), null).Should().BeFalse();
		Constants.Functions.FormFieldIsSensitive<Item>(null, null).Should().BeFalse();
	}

	/// <summary>An explicit field description wins over the Display attribute.</summary>
	[Fact]
	public void FormFieldDescription_PrefersExplicitDescription()
	{
		var field = new FormField<Item> { Field = x => x.Described, Description = "explicit" };

		Constants.Functions.FormFieldDescription(field, null).Should().Be("explicit");
	}

	/// <summary>Without an explicit description the Display attribute description is used.</summary>
	[Fact]
	public void FormFieldDescription_FallsBackToDisplayAttribute()
	{
		var field = new FormField<Item> { Field = x => x.Described };

		Constants.Functions.FormFieldDescription(field, null).Should().Be("From attribute");
	}

	/// <summary>With neither an explicit description nor an attribute the result is empty.</summary>
	[Fact]
	public void FormFieldDescription_WithNoSource_IsEmpty()
	{
		Constants.Functions.FormFieldDescription(new FormField<Item> { Field = x => x.Plain }, null).Should().BeEmpty();
		Constants.Functions.FormFieldDescription(new FormField<Item>(), null).Should().BeEmpty();
	}

	/// <summary>An item type used by the description tests.</summary>
	public sealed class Item
	{
		/// <summary>Gets or sets a property with a Display description.</summary>
		[Display(Description = "From attribute")]
		public string Described { get; set; } = string.Empty;

		/// <summary>Gets or sets a property without attributes.</summary>
		public string Plain { get; set; } = string.Empty;
	}
}
