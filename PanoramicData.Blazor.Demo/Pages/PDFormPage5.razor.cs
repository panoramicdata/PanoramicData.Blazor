using BlazorMonaco.Editor;
using System.Diagnostics.CodeAnalysis;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFormPage5
{
	private readonly DelegatedDataProvider<DatabaseQueryModel> _dataProvider = new()
	{
		UpdateAsync = (model, delta, cancellationToken) =>
		{
			return Task.FromResult(new OperationResponse { Success = true });

		}
	};

	private readonly DatabaseQueryModel _model = new DatabaseQueryModel
	{
		SqlQuery = "SELECT * \r\n  FROM [Customers]\r\n WHERE [Type] = 123"
	};

	[AllowNull]
	private PDForm<DatabaseQueryModel> _queryForm;
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

	public PDFormPage5()
	{
	}

	private async Task OnBeginEditAsync()
	{
		await _queryForm.EditItemAsync(_model, FormModes.Edit);
	}


	private async Task OnFooterClick(string key)
	{
		if (_queryForm != null)
		{
			if (key == "Save")
			{
			}
			await _queryForm.EditItemAsync(null, FormModes.ReadOnly).ConfigureAwait(true);
		}
	}
}
