using PanoramicData.Blazor.Services;
using System.ComponentModel.DataAnnotations;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFormPage6
{
	private PDForm<FormColoursModel>? _form;
	private bool _dark;
	private bool _neutralLabels;
	private bool _labelEdges;
	private bool _quietStatus;
	private bool _mutedHelpers;
	private bool _footerBox;

	private readonly DelegatedDataProviderService<FormColoursModel> _dataProvider = new()
	{
		CreateAsync = (_, _) => Task.FromResult(new OperationResponse { Success = true })
	};

	private readonly FormFieldHelper<FormColoursModel> _folderHelper = new()
	{
		IconCssClass = "fas fa-fw fa-folder-open",
		ToolTip = "Choose a folder",
		Click = _ => new FormFieldResult { NewValue = "/Reports" }
	};

	private string WrapperCssClass => string.Join(' ', new[]
	{
		_neutralLabels ? "neutral-labels" : null,
		_labelEdges ? "label-edges" : null,
		_quietStatus ? "quiet-status" : null,
		_mutedHelpers ? "muted-helpers" : null,
		_footerBox ? "footer-box" : null
	}.Where(x => x is not null));

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await ResetFormAsync().ConfigureAwait(true);
		}
	}

	private async Task ResetFormAsync()
	{
		if (_form is not null)
		{
			await _form.EditItemAsync(new FormColoursModel(), FormModes.Create).ConfigureAwait(true);
		}
	}

	private void AllOn() => SetAll(true);

	private void AllOff() => SetAll(false);

	private void SetAll(bool value)
	{
		_neutralLabels = value;
		_labelEdges = value;
		_quietStatus = value;
		_mutedHelpers = value;
		_footerBox = value;
	}

	/// <summary>
	/// A schedule-like item with required fields, a valid one and an invalid one, to show every validation state.
	/// </summary>
	public class FormColoursModel
	{
		/// <summary>Gets or sets the name, which is required.</summary>
		[Required]
		public string Name { get; set; } = string.Empty;

		/// <summary>Gets or sets the description, which starts valid.</summary>
		public string Description { get; set; } = "Weekly summary";

		/// <summary>Gets or sets the input folder, which is required.</summary>
		[Required]
		public string InputFolder { get; set; } = string.Empty;

		/// <summary>Gets or sets the output folder, which is required.</summary>
		[Required]
		public string OutputFolder { get; set; } = string.Empty;

		/// <summary>Gets or sets the address to notify, which starts invalid.</summary>
		[EmailAddress]
		public string NotifyEmail { get; set; } = "not-an-address";

		/// <summary>Gets or sets free-text notes.</summary>
		public string Notes { get; set; } = "Copy me";
	}
}
