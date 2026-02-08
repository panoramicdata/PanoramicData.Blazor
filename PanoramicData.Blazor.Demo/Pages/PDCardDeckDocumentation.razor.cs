namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDCardDeckDocumentation
{
	private const string _example1Code = """
		<PDCardDeck TItem="Product"
		            Items="_products"
		            MinCardWidth="200">
		    <CardTemplate Context="item">
		        <div class="card">
		            <div class="card-body">
		                <h5>@item.Name</h5>
		                <p>@item.Description</p>
		            </div>
		        </div>
		    </CardTemplate>
		</PDCardDeck>
		""";
}
