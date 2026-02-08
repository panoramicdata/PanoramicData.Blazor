using PanoramicData.Blazor.Demo.Data;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTableDocumentation
{
	private readonly PersonDataProvider _simpleDataProvider = new();

	private const string _example1Code = """
		<PDTable TItem="Person"
		         DataProvider="_dataProvider"
		         KeyField="x => x.Id">
		    <PDColumn TItem="Person" 
		              Field="x => x.Id" 
		              Title="ID" 
		              Width="80" />
		    <PDColumn TItem="Person" 
		              Field="x => x.FirstName" 
		              Title="First Name" />
		    <PDColumn TItem="Person" 
		              Field="x => x.LastName" 
		              Title="Last Name" />
		    <PDColumn TItem="Person" 
		              Field="x => x.Email" 
		              Title="Email" />
		</PDTable>

		@code {
		    private PersonDataProvider _dataProvider = new();
		}
		""";

	private const string _example2Code = """
		<PDTable TItem="Person"
		         DataProvider="_dataProvider"
		         KeyField="x => x.Id"
		         AllowSort="true">
		    <PDColumn TItem="Person" 
		              Field="x => x.Id" 
		              Title="ID" 
		              Sortable="true" />
		    <PDColumn TItem="Person" 
		              Field="x => x.FirstName" 
		              Title="First Name" 
		              Sortable="true" />
		    <PDColumn TItem="Person" 
		              Field="x => x.LastName" 
		              Title="Last Name" 
		              Sortable="true" />
		</PDTable>
		""";

	private const string _example3Code = """
		<PDTable TItem="Person"
		         DataProvider="_dataProvider"
		         KeyField="x => x.Id">
		    <PDColumn TItem="Person" 
		              Field="x => x.FirstName" 
		              Title="Name">
		        <Template>
		            <strong>@context.FirstName</strong> 
		            @context.LastName
		        </Template>
		    </PDColumn>
		    <PDColumn TItem="Person" 
		              Field="x => x.Email" 
		              Title="Contact">
		        <Template>
		            <a href="mailto:@context.Email">
		                @context.Email
		            </a>
		        </Template>
		    </PDColumn>
		</PDTable>
		""";
}
