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

		private FormModes Form1Mode { get; set; }

		private Person Person1 { get; set; }

		public PDFormPage()
		{
			var people = PersonDataProvider
				.GetDataAsync(new Services.DataRequest<Person>(), CancellationToken.None)
				.ContinueWith(PopulatePeopleResult);
		}

		private void PopulatePeopleResult(Task<DataResponse<Person>> resultTask)
		{
			if (!resultTask.IsFaulted)
			{
				Person1 = resultTask.Result.Items.FirstOrDefault();
				InvokeAsync(() => StateHasChanged());
			}
		}
    }
}
