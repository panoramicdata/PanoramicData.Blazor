namespace PanoramicData.Blazor.Demo.Shared
{
	public partial class ExampleTicket
	{

		[Parameter]
		public required Todo Ticket { get; set; }


		private static string GetSeverityIcon(Todo todo)
		{
			var priority = todo.Priority;

			return priority switch
			{
				Priority.Minor => "fa fa-info-circle",
				Priority.Major => "fa fa-exclamation-circle",
				Priority.Critical => "fa fa-exclamation-triangle",
				Priority.Blocker => "fa fa-fire",
				_ => "fa fa-check-circle"
			};
		}
	}
}