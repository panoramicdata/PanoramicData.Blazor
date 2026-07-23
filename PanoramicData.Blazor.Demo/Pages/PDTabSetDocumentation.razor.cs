namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTabSetDocumentation
{
	private const string _example1Code = """
		<PDTabSet>
		    <PDTab Title="Tab 1">
		        <p class="p-3">Content for Tab 1</p>
		    </PDTab>
		    <PDTab Title="Tab 2">
		        <p class="p-3">Content for Tab 2</p>
		    </PDTab>
		    <PDTab Title="Tab 3">
		        <p class="p-3">Content for Tab 3</p>
		    </PDTab>
		</PDTabSet>
		""";

	private const string _example2Code = """
		<PDTabSet>
		    <PDTab Title="Home" IconCssClass="bi bi-house-fill">
		        <p class="p-3">Home content</p>
		    </PDTab>
		    <PDTab Title="Settings" IconCssClass="bi bi-gear-fill">
		        <p class="p-3">Settings content</p>
		    </PDTab>
		    <PDTab Title="Reports" IconCssClass="bi bi-bar-chart-fill">
		        <p class="p-3">Reports content</p>
		    </PDTab>
		</PDTabSet>
		""";

	private const string _example3Code = """
		<PDTabSet IsTabReorderingEnabled="true"
		          OnTabsReordered="OnTabsReordered">
		    <PDTab Title="First"><p class="p-3">First tab</p></PDTab>
		    <PDTab Title="Second"><p class="p-3">Second tab</p></PDTab>
		    <PDTab Title="Third"><p class="p-3">Third tab</p></PDTab>
		</PDTabSet>

		@code {
		    // Mirror the new order into your own list so Blazor re-renders correctly
		    private void OnTabsReordered(IReadOnlyList<PDTab> tabs)
		    {
		        var reordered = tabs
		            .Select(t => _myTabs.FirstOrDefault(m => m.Id == t.Id))
		            .OfType<MyTabModel>()
		            .ToList();
		        _myTabs.Clear();
		        _myTabs.AddRange(reordered);
		    }
		}
		""";
}
