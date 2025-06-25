namespace PanoramicData.Blazor.Demo.Shared
{
	public partial class SeverityIcon
	{
		[Parameter]
		public required bool Interactable { get; set; }

		[Parameter]
		public required Todo Ticket { get; set; }

		/// <summary>
		/// Returns the Icon for the severity of the ticket.
		/// </summary>
		/// <param name="todo"></param>
		/// <returns></returns>
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

		private bool _isHovered;

		/// <summary>
		/// Shows the user the color of the severity icon when hovered over.
		/// </summary>
		/// <param name="ticket"></param>
		/// <param name="animate">Whether to show animations or not</param>
		/// <returns></returns>
		private string GetHoverSeverityClass(Todo ticket, bool animate)
		{
			if (!_isHovered || !Interactable)
			{
				return string.Empty;
			}

			var priority = ticket.Priority switch
			{
				Priority.Trivial => "trivial",
				Priority.Minor => "minor",
				Priority.Major => "major",
				Priority.Critical => "critical",
				Priority.Blocker => "blocker",
				_ => string.Empty
			};

			// Returns the priority and the animate class, it passed as an argument
			return priority + ((animate) ? " animate" : "");
		}
	}
}