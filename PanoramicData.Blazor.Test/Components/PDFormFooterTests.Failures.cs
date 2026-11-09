using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// A failure behind a footer button goes to the form, never to Blazor, where it would end the user's circuit.
/// </summary>
public partial class PDFormFooterTests
{
	/// <summary>A data provider that throws while saving hands the exception to the form's exception handler.</summary>
	[Fact]
	public async Task Save_WhenTheDataProviderThrows_GivesTheFormTheException()
	{
		var handled = new List<Exception>();
		var form = Render<PDForm<Person>>(parameters => parameters
			.Add(p => p.DefaultMode, FormModes.Create)
			.Add(p => p.Item, new Person { Name = "Ada" })
			.Add(p => p.DataProvider, new ThrowingProvider())
			.Add(p => p.ExceptionHandler, (Exception ex) => handled.Add(ex))
			.AddChildContent<PDFormFooter<Person>>());

		await ButtonFor(form, "Save").ClickAsync(new());

		handled.Should().ContainSingle().Which.Should().BeOfType<HttpRequestException>();
		Renderer.UnhandledException.IsCompleted.Should().BeFalse();
	}

	/// <summary>A host Click handler that throws hands the exception to the form's exception handler.</summary>
	[Fact]
	public async Task Cancel_WhenTheHostsClickHandlerThrows_GivesTheFormTheException()
	{
		var handled = new List<Exception>();
		var form = Render<PDForm<Person>>(parameters => parameters
			.Add(p => p.DefaultMode, FormModes.Edit)
			.Add(p => p.Item, new Person { Name = "Ada" })
			.Add(p => p.ExceptionHandler, (Exception ex) => handled.Add(ex))
			.AddChildContent<PDFormFooter<Person>>(footer => footer
				.Add(f => f.Click, (string _) => throw new InvalidOperationException("The host failed"))));

		await ButtonFor(form, "Cancel").ClickAsync(new());

		handled.Should().ContainSingle().Which.Message.Should().Be("The host failed");
		Renderer.UnhandledException.IsCompleted.Should().BeFalse();
	}

	/// <summary>A data provider whose create fails as a broken connection would.</summary>
	private sealed class ThrowingProvider : DataProviderBase<Person>
	{
		public override Task<OperationResponse> CreateAsync(Person item, CancellationToken cancellationToken)
			=> throw new HttpRequestException("The server is down");
	}
}
