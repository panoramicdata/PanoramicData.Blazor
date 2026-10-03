using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFormPage4
{
	private readonly FormFieldHelper<RegisterModel> _docxHelper;
	private readonly FormFieldHelper<RegisterModel> _htmlHelper;
	private readonly FormFieldHelper<RegisterModel> _pdfHelper;
	private readonly FormFieldHelper<RegisterModel> _xlsxHelper;

	private readonly DelegatedDataProviderService<RegisterModel> _dataProvider = new()
	{
		CreateAsync = (_, _) => Task.FromResult(new OperationResponse())
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

	protected PDForm<RegisterModel>? RegisterForm { get; set; }

	public PDFormPage4()
	{
		_docxHelper = new FormFieldHelper<RegisterModel>
		{
			ClickAsync = async (x) =>
			{
				await RegisterForm!.SetFieldValueAsync(x, !RegisterForm.GetFieldValue<bool>(nameof(RegisterModel.ReportFormatDocx), true));
				return new FormFieldResult();
			},
			IconCssClass = "fas -fa-fw fa-toggle-on c-docx"
		};
		_htmlHelper = new FormFieldHelper<RegisterModel>
		{
			ClickAsync = async (x) =>
			{
				await RegisterForm!.SetFieldValueAsync(x, !RegisterForm.GetFieldValue<bool>(nameof(RegisterModel.ReportFormatHtml), true));
				return new FormFieldResult();
			},
			IconCssClass = "fas -fa-fw fa-toggle-on c-html"
		};
		_pdfHelper = new FormFieldHelper<RegisterModel>
		{
			ClickAsync = async (x) =>
			{
				await RegisterForm!.SetFieldValueAsync(x, !RegisterForm.GetFieldValue<bool>(nameof(RegisterModel.ReportFormatPdf), true));
				return new FormFieldResult();
			},
			IconCssClass = "fas -fa-fw fa-toggle-on c-pdf"
		};
		_xlsxHelper = new FormFieldHelper<RegisterModel>
		{
			ClickAsync = async (x) =>
			{
				await RegisterForm!.SetFieldValueAsync(x, !RegisterForm.GetFieldValue<bool>(nameof(RegisterModel.ReportFormatXlsx), true));
				return new FormFieldResult();
			},
			IconCssClass = "fas -fa-fw fa-toggle-on c-xlsx"
		};
	}

}
