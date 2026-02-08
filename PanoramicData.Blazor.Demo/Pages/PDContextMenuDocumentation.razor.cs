namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDContextMenuDocumentation
{
	private const string _example1Code = """
		<PDContextMenu Items="_menuItems" 
		               ItemClick="OnItemClick">
		    <ChildContent>
		        <div class="p-4 bg-light">
		            Right-click here
		        </div>
		    </ChildContent>
		</PDContextMenu>

		@code {
		    private List<MenuItem> _menuItems = new()
		    {
		        new("Cut", iconCssClass: "fas fa-cut"),
		        new("Copy", iconCssClass: "fas fa-copy"),
		        new("Paste", iconCssClass: "fas fa-paste"),
		        new("-"),
		        new("Delete", iconCssClass: "fas fa-trash")
		    };

		    private void OnItemClick(MenuItem item)
		    {
		        // Handle menu item click
		    }
		}
		""";
}
