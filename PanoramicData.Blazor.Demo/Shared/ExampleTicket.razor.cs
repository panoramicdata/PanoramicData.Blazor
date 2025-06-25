namespace PanoramicData.Blazor.Demo.Shared
{
	public partial class ExampleTicket
	{

		[Parameter]
		public required Todo Ticket { get; set; }

		[Parameter]
		public bool ShowEditOptions { get; set; }


	}
}