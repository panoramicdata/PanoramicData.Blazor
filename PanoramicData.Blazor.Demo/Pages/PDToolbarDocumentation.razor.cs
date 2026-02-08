namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDToolbarDocumentation
{
	private string _searchText = "";

	private const string _example1Code = """
		<PDToolbar>
		    <PDToolbarButton Text="New" 
		                     CssClass="btn-primary" 
		                     IconCssClass="fas fa-plus" />
		    <PDToolbarButton Text="Edit" 
		                     CssClass="btn-secondary" 
		                     IconCssClass="fas fa-edit" />
		    <PDToolbarButton Text="Delete" 
		                     CssClass="btn-danger" 
		                     IconCssClass="fas fa-trash" />
		</PDToolbar>
		""";

	private const string _example2Code = """
		<PDToolbar>
		    <PDToolbarDropdown Text="File" 
		                       IconCssClass="fas fa-file">
		        <PDMenuItem Text="New" 
		                    IconCssClass="fas fa-plus" />
		        <PDMenuItem Text="Open" 
		                    IconCssClass="fas fa-folder-open" />
		        <PDMenuItem Text="Save" 
		                    IconCssClass="fas fa-save" />
		        <PDMenuItemSeparator />
		        <PDMenuItem Text="Exit" 
		                    IconCssClass="fas fa-times" />
		    </PDToolbarDropdown>
		    <PDToolbarDropdown Text="Edit" 
		                       IconCssClass="fas fa-edit">
		        <PDMenuItem Text="Undo" 
		                    IconCssClass="fas fa-undo" />
		        <PDMenuItem Text="Redo" 
		                    IconCssClass="fas fa-redo" />
		    </PDToolbarDropdown>
		</PDToolbar>
		""";

	private const string _example3Code = """
		<PDToolbar>
		    <PDToolbarButton Text="Refresh" 
		                     CssClass="btn-primary" />
		    <PDToolbarSeparator />
		    <PDToolbarTextbox Label="Search:" 
		                      @bind-Value="_searchText" 
		                      ShowClearButton="true" />
		    <PDToolbarButton Text="Settings" 
		                     CssClass="btn-secondary" 
		                     ShiftRight="true" />
		</PDToolbar>

		@code {
		    private string _searchText = "";
		}
		""";
}
