using PanoramicData.Blazor.Services;
using BlazorMonaco.Editor;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFormPage5
{
	protected PDForm<DatabaseQueryModel>? QueryForm { get; set; }
	private readonly DelegatedDataProviderService<DatabaseQueryModel> _dataProvider = new()
	{
		UpdateAsync = (_, _, _) => Task.FromResult(new OperationResponse { Success = true })
	};
	private readonly DatabaseQueryModel _model = new()
	{
		SqlQuery = "SELECT *\r\n  FROM [Customers]\r\n WHERE [Type] = 123"
	};

	private static FieldStringOptions QueryEditorOptions
	{
		get
		{
			return new FieldStringOptions
			{
				CssClass = "",
				Editor = FieldStringOptions.Editors.Monaco,
				MonacoOptions = (_) => new StandaloneEditorConstructionOptions
				{
					AutomaticLayout = true,
					Language = "sql"
				},
				Resize = true,
				ResizeCssCls = "mh-200-px"
			};
		}
	}

	private async Task OnBeginEditAsync()
	{
		if (QueryForm != null)
		{
			await QueryForm.EditItemAsync(_model, FormModes.Edit, false);
		}
	}

	private async Task OnFieldUpdatedAsync(FieldUpdateArgs<DatabaseQueryModel> args)
	{
		if (QueryForm != null && args.Field.Name == "EmailAddress")
		{
			var field = QueryForm.Fields.FirstOrDefault(x => x.GetName() == "SqlQuery");
			if (field != null)
			{
				await QueryForm.SetFieldValueAsync(field, $"SELECT *\r\n  FROM [Customers]\r\n WHERE [EmailAddress] = '{args.NewValue}'");
			}
		}
	}


	private async Task OnFooterClick(string key)
	{
		if (QueryForm != null)
		{
			if (key == "No")
			{
				return;
			}

			await QueryForm.EditItemAsync(null, FormModes.ReadOnly).ConfigureAwait(true);
		}
	}
}
