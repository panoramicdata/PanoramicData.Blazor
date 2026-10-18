namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDCardDeckDocumentation : ComponentBase
{
	private const string _example1Code = """
		<PDCardDeck TCard="Ticket"
		            DataFunction="GetTicketsAsync">
		    <CardTemplate Context="ticket">
		        <h6>@ticket.Title</h6>
		        <p>@ticket.Description</p>
		    </CardTemplate>
		</PDCardDeck>
		""";
}
