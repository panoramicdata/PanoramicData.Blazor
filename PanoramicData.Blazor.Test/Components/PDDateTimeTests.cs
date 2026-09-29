using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDDateTime"/> renders date and time inputs and parses what the user types into them.
/// </summary>
public class PDDateTimeTests : BunitContext
{
	private static readonly DateTime _value = new(2026, 3, 14, 15, 9, 26);
	private readonly List<DateTime> _changes = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDDateTimeTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<PDDateTime> RenderDateTime(string? dateFormat = null, bool showTime = false)
		=> Render<PDDateTime>(parameters =>
		{
			parameters
				.Add(p => p.Value, _value)
				.Add(p => p.ShowTime, showTime)
				.Add(p => p.ValueChanged, (DateTime value) => _changes.Add(value));
			if (dateFormat != null)
			{
				parameters.Add(p => p.DateFormat, dateFormat);
			}
		});

	/// <summary>With the default format the native date picker is used, showing an ISO date.</summary>
	[Fact]
	public void DefaultFormat_UsesTheNativeDatePicker()
	{
		var component = RenderDateTime();

		var input = component.Find("input.date");
		input.GetAttribute("type").Should().Be("date");
		input.GetAttribute("value").Should().Be("2026-03-14");
		input.HasAttribute("placeholder").Should().BeFalse();
		component.FindAll("input.time").Should().BeEmpty();
	}

	/// <summary>A custom format uses a text box, formats the value with it and shows it as a placeholder.</summary>
	[Fact]
	public void CustomFormat_UsesATextBox()
	{
		var component = RenderDateTime("dd.MM.yyyy");

		var input = component.Find("input.date");
		input.GetAttribute("type").Should().Be("text");
		input.GetAttribute("value").Should().Be("14.03.2026");
		input.GetAttribute("placeholder").Should().Be("dd.MM.yyyy");
	}

	/// <summary>ShowTime adds a time input with the given step.</summary>
	[Fact]
	public void ShowTime_AddsATimeInput()
	{
		var component = Render<PDDateTime>(parameters => parameters
			.Add(p => p.Value, _value)
			.Add(p => p.ShowTime, true)
			.Add(p => p.TimeStepSecs, 60));

		var time = component.Find("input.time");
		time.GetAttribute("type").Should().Be("time");
		time.GetAttribute("value").Should().Be("15:09:26");
		time.GetAttribute("step").Should().Be("60");
	}

	/// <summary>A disabled component disables both inputs; a hidden one is given d-none.</summary>
	[Fact]
	public void DisabledAndHidden_AreReflected()
	{
		var component = Render<PDDateTime>(parameters => parameters
			.Add(p => p.Value, _value)
			.Add(p => p.ShowTime, true)
			.Add(p => p.IsEnabled, false)
			.Add(p => p.IsVisible, false)
			.Add(p => p.CssClass, "extra"));

		component.FindAll("input").Should().AllSatisfy(i => i.HasAttribute("disabled").Should().BeTrue());
		component.Find("div.pddatetime").ClassList.Should().Contain(["d-none", "extra"]);
	}

	/// <summary>Typing a valid date keeps the time of day and raises ValueChanged.</summary>
	[Fact]
	public void ValidDateInput_KeepsTheTime_AndRaisesValueChanged()
	{
		var component = RenderDateTime();

		component.Find("input.date").Input("2027-01-02");

		_changes.Should().Equal(new DateTime(2027, 1, 2, 15, 9, 26));
		component.Instance.Value.Should().Be(new DateTime(2027, 1, 2, 15, 9, 26));
		component.Find("input.date").ClassList.Should().NotContain("invalid");
	}

	/// <summary>A valid date typed in a custom format is parsed with that format.</summary>
	[Fact]
	public void ValidDateInput_InACustomFormat_IsParsedWithIt()
	{
		var component = RenderDateTime("dd.MM.yyyy");

		component.Find("input.date").Input("02.01.2027");

		_changes.Should().Equal(new DateTime(2027, 1, 2, 15, 9, 26));
	}

	/// <summary>An unparseable date marks the input invalid and raises nothing; a later valid one clears it.</summary>
	[Fact]
	public void InvalidDateInput_MarksTheInputInvalid_UntilCorrected()
	{
		var component = RenderDateTime();

		component.Find("input.date").Input("not a date");
		component.Find("input.date").ClassList.Should().Contain("invalid");
		_changes.Should().BeEmpty();

		component.Find("input.date").Input("2027-01-02");
		component.Find("input.date").ClassList.Should().NotContain("invalid");
	}

	/// <summary>A null date input is treated as invalid.</summary>
	[Fact]
	public void NullDateInput_IsInvalid()
	{
		var component = RenderDateTime();

		component.Find("input.date").Input((object?)null);

		component.Find("input.date").ClassList.Should().Contain("invalid");
		_changes.Should().BeEmpty();
	}

	/// <summary>Typing a valid time keeps the date and raises ValueChanged.</summary>
	[Fact]
	public void ValidTimeInput_KeepsTheDate_AndRaisesValueChanged()
	{
		var component = RenderDateTime(showTime: true);

		component.Find("input.time").Input("08:30:00");

		_changes.Should().Equal(new DateTime(2026, 3, 14, 8, 30, 0));
		component.Find("input.time").ClassList.Should().NotContain("invalid");
	}

	/// <summary>An unparseable or null time marks the time input invalid and raises nothing.</summary>
	[Theory]
	[InlineData("25:99")]
	[InlineData(null)]
	public void InvalidTimeInput_MarksTheTimeInputInvalid(string? text)
	{
		var component = RenderDateTime(showTime: true);

		component.Find("input.time").Input(text);

		component.Find("input.time").ClassList.Should().Contain("invalid");
		_changes.Should().BeEmpty();
	}

	/// <summary>Leaving either input raises Blur.</summary>
	[Fact]
	public void Blur_OnEitherInput_RaisesBlur()
	{
		var blurs = 0;
		var component = Render<PDDateTime>(parameters => parameters
			.Add(p => p.ShowTime, true)
			.Add(p => p.Blur, () => blurs++));

		component.Find("input.date").Blur();
		component.Find("input.time").Blur();

		blurs.Should().Be(2);
	}
}
