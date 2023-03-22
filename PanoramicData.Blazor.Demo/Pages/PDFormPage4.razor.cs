using System.Diagnostics.CodeAnalysis;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFormPage4
{
	private readonly DelegatedDataProvider<RegisterModel> _dataProvider = new()
	{
		CreateAsync = (model, cancellationToken) =>
		{
			var result = new OperationResponse();
			return Task.FromResult(result);
		}
	};

	private readonly FieldBooleanOptions _reportFormatDisplayOptions = new FieldBooleanOptions
	{
		CssClass = "form-control",
		LabelBefore = true,
		OffText = "No",
		OnText = "Yes",
		Rounded = true,
		Style = FieldBooleanOptions.DisplayComponent.ToggleSwitch
	};

	private readonly RegisterModel _model = new();

	[AllowNull]
	private PDForm<RegisterModel> _registerForm;

}
