namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTabSetDocumentation : ComponentBase
{
	private static readonly Guid _firstTabId = new("3c9a1d0e-5b7f-4e21-8a64-0d2f9b6c1e01");
	private static readonly Guid _secondTabId = new("3c9a1d0e-5b7f-4e21-8a64-0d2f9b6c1e02");

	private Guid? _selectedTabId = _firstTabId;

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
		    <PDTab Title="Home" IconCssClass="fas fa-home">
		        <p class="p-3">Home content</p>
		    </PDTab>
		    <PDTab Title="Settings" IconCssClass="fas fa-cog">
		        <p class="p-3">Settings content</p>
		    </PDTab>
		    <PDTab Title="Reports" IconCssClass="fas fa-chart-bar">
		        <p class="p-3">Reports content</p>
		    </PDTab>
		</PDTabSet>
		""";

	private const string _example4Code = """
		<button @onclick="() => _selectedTabId = _secondTabId">Select second tab</button>

		<PDTabSet @bind-SelectedTabId="_selectedTabId">
		    <PDTab Id="_firstTabId" Title="First"><p class="p-3">First tab</p></PDTab>
		    <PDTab Id="_secondTabId" Title="Second"><p class="p-3">Second tab</p></PDTab>
		</PDTabSet>

		@code {
		    private static readonly Guid _firstTabId = Guid.Parse("3c9a1d0e-5b7f-4e21-8a64-0d2f9b6c1e01");
		    private static readonly Guid _secondTabId = Guid.Parse("3c9a1d0e-5b7f-4e21-8a64-0d2f9b6c1e02");
		    private Guid? _selectedTabId = _firstTabId;
		}
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
