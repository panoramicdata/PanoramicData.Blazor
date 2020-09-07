using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using PanoramicData.Blazor.Web.Data;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Web.Pages
{
    public partial class PDFormPage
    {
		private readonly PersonDataProvider PersonDataProvider = new PersonDataProvider();

		// properties for unlinked example
		private PDForm<Person> Form1 { get; set; } = null!;
		private FormModes Form1Mode { get; set; }
		private List<Person> People { get; set; } = new List<Person>();
		private Person SelectedPerson { get; set; }

		public PDFormPage()
		{
			PersonDataProvider
				.GetDataAsync(new Services.DataRequest<Person>(), CancellationToken.None)
				.ContinueWith(PopulatePeopleResult);
		}

		private void PopulatePeopleResult(Task<DataResponse<Person>> resultTask)
		{
			if (!resultTask.IsFaulted)
			{
				People.AddRange(resultTask.Result.Items);
				//Person = resultTask.Result.Items.FirstOrDefault();
				InvokeAsync(() => StateHasChanged());
			}
		}

		private void OnFooterClick(string key)
		{
			if(key == "Cancel")
			{
				SelectedPerson = null;
			}
		}
	}
}
