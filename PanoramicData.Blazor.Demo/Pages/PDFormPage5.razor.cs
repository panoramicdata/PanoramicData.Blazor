using BlazorMonaco.Editor;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFormPage5
{
	private PDForm<DatabaseQueryModel>? _queryForm;
	private readonly DelegatedDataProvider<DatabaseQueryModel> _dataProvider = new()
	{
		UpdateAsync = (model, delta, cancellationToken) =>
		{
			return Task.FromResult(new OperationResponse { Success = true });

		}
	};
	private readonly DatabaseQueryModel _model = new()
	{
		SqlQuery = "SELECT *\r\n  FROM [Customers]\r\n WHERE [Type] = 123"
	};

	private FieldStringOptions QueryEditorOptions
	{
		get
		{
			return new FieldStringOptions
			{
				CssClass = "h-300",
				Editor = FieldStringOptions.Editors.Monaco,
				MonacoOptions = (_) => new StandaloneEditorConstructionOptions
				{
					AutomaticLayout = true,
					Language = "sql"
				}
			};
		}
	}

	private async Task OnBeginEditAsync()
	{
		if (_queryForm != null)
		{
			await _queryForm.EditItemAsync(_model, FormModes.Edit);
		}
	}


	private async Task OnFooterClick(string key)
	{
		if (_queryForm != null)
		{
			if (key == "No")
			{
				return;
			}
			await _queryForm.EditItemAsync(null, FormModes.ReadOnly).ConfigureAwait(true);
		}
	}
}
