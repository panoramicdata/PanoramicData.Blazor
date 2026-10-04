using AngleSharp.Dom;
using Bunit;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDFormFooter{TItem}"/> shows the buttons appropriate to its form's mode, reports
/// validation errors, and drives the save, delete and cancel journeys through the form.
/// </summary>
public partial class PDFormFooterTests : BunitContext
{
	/// <summary>
	/// How long to wait for a render that another thread or a timer brings about. Generous because a busy
	/// machine (the whole suite under coverage) can hold the renderer's dispatcher well past bUnit's default.
	/// </summary>
	private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

	/// <summary>Sets up the rendering context.</summary>
	public PDFormFooterTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private (IRenderedComponent<PDForm<Person>> Form, Person Person) RenderForm(
		RecordingProvider? provider,
		FormModes mode = FormModes.Create,
		Action<ComponentParameterCollectionBuilder<PDFormFooter<Person>>>? configure = null)
	{
		var person = new Person { Name = "Ada" };
		var form = Render<PDForm<Person>>(parameters =>
		{
			parameters
				.Add(p => p.DefaultMode, mode)
				.Add(p => p.Item, person)
				.AddChildContent<PDFormFooter<Person>>(footer => configure?.Invoke(footer));
			if (provider is not null)
			{
				parameters.Add(p => p.DataProvider, provider);
			}
		});
		return (form, person);
	}

	private static Task EditAsync(IRenderedComponent<PDForm<Person>> form, FormModes mode)
		=> form.InvokeAsync(() => form.Instance.EditItemAsync(form.Instance.Item, mode));

	private static IElement ButtonFor(IRenderedComponent<PDForm<Person>> form, string text)
		=> form.FindAll(".pdtoolbaritem:not(.pd-hidden) button").Single(b => b.TextContent.Trim() == text);

	private static List<string> VisibleButtonTexts(IRenderedComponent<PDForm<Person>> form)
		=> [.. form.FindAll(".pdtoolbaritem:not(.pd-hidden) button").Select(b => b.TextContent.Trim())];

	/// <summary>The item edited by the form.</summary>
	public sealed class Person
	{
		/// <summary>Gets or sets the name.</summary>
		public string Name { get; set; } = string.Empty;
	}

	/// <summary>A data provider that records what it was asked to do and succeeds or fails on request.</summary>
	private sealed class RecordingProvider : DataProviderBase<Person>
	{
		public bool Succeeds { get; init; } = true;

		public List<Person> Created { get; } = [];

		public List<Person> Updated { get; } = [];

		public List<Person> Deleted { get; } = [];

		public override Task<OperationResponse> CreateAsync(Person item, CancellationToken cancellationToken)
			=> Record(Created, item, cancellationToken);

		public override Task<OperationResponse> UpdateAsync(Person item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(delta);
			return Record(Updated, item, cancellationToken);
		}

		public override Task<OperationResponse> DeleteAsync(Person item, CancellationToken cancellationToken)
			=> Record(Deleted, item, cancellationToken);

		private Task<OperationResponse> Record(List<Person> log, Person item, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			log.Add(item);
			return Task.FromResult(new OperationResponse { Success = Succeeds, ErrorMessage = Succeeds ? string.Empty : "Refused" });
		}
	}
}
