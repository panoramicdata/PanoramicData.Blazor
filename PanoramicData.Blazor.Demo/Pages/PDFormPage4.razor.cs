using System.Diagnostics.CodeAnalysis;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFormPage4
{
	private readonly FormFieldHelper<RegisterModel> _docxHelper;
	private readonly FormFieldHelper<RegisterModel> _htmlHelper;
	private readonly FormFieldHelper<RegisterModel> _pdfHelper;
	private readonly FormFieldHelper<RegisterModel> _xlsxHelper;

	private readonly DelegatedDataProvider<RegisterModel> _dataProvider = new()
	{
		CreateAsync = (model, cancellationToken) =>
		{
			var result = new OperationResponse();
			return Task.FromResult(result);
		}
	};

	private readonly FieldBooleanOptions _reportFormatDisplayOptions = new()
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

	public PDFormPage4()
	{
		_docxHelper = new FormFieldHelper<RegisterModel>
		{
			ClickAsync = async (x) =>
			{
				await _registerForm!.SetFieldValueAsync(x, !_registerForm.GetFieldValue<bool>(nameof(RegisterModel.ReportFormatDocx), true));
				return new FormFieldResult();
			},
			IconCssClass = "fas -fa-fw fa-toggle-on c-docx"
		};
		_htmlHelper = new FormFieldHelper<RegisterModel>
		{
			ClickAsync = async (x) =>
			{
				await _registerForm!.SetFieldValueAsync(x, !_registerForm.GetFieldValue<bool>(nameof(RegisterModel.ReportFormatHtml), true));
				return new FormFieldResult();
			},
			IconCssClass = "fas -fa-fw fa-toggle-on c-html"
		};
		_pdfHelper = new FormFieldHelper<RegisterModel>
		{
			ClickAsync = async (x) =>
			{
				await _registerForm!.SetFieldValueAsync(x, !_registerForm.GetFieldValue<bool>(nameof(RegisterModel.ReportFormatPdf), true));
				return new FormFieldResult();
			},
			IconCssClass = "fas -fa-fw fa-toggle-on c-pdf"
		};
		_xlsxHelper = new FormFieldHelper<RegisterModel>
		{
			ClickAsync = async (x) =>
			{
				await _registerForm!.SetFieldValueAsync(x, !_registerForm.GetFieldValue<bool>(nameof(RegisterModel.ReportFormatXlsx), true));
				return new FormFieldResult();
			},
			IconCssClass = "fas -fa-fw fa-toggle-on c-xlsx"
		};
	}

}
