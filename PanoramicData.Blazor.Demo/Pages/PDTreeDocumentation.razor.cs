namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTreeDocumentation
{
	private const string _example1Code = """
		<PDTree TItem="MyItem"
		        DataProvider="_dataProvider"
		        KeyField="x => x.Id"
		        TextField="x => x.Name"
		        IsLeaf="x => !x.HasChildren"
		        ShowRoot="true" />

		@code {
		    // DataProvider must implement 
		    // IDataProviderService<MyItem>
		}
		""";
}
