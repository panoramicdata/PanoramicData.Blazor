namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDDragPanelDocumentation
{
	private const string _example1Code = """
		<PDDragContainer Items="_items"
		                 TItem="MyItem">
		    <PDDragPanel TItem="MyItem"
		                 ItemOrderChanged="OnOrderChanged" />
		</PDDragContainer>
		""";
}
