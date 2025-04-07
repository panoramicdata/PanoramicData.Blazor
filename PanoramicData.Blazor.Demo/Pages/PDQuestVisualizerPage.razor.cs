using PanoramicData.Blazor.Models.Quests;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDQuestVisualizerPage : IAsyncDisposable
{
	private IJSObjectReference? _commonModule;

	private readonly List<Quest> _quests = DemoData.GetSampleQuests();
	private readonly List<QuestAction> _questActions = DemoData.GetSampleQuestActions();

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	protected override async Task OnInitializedAsync()
	{
		_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/js/common.js").ConfigureAwait(true);
	}

	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_commonModule != null)
			{
				await _commonModule.DisposeAsync().ConfigureAwait(true);
			}
		}
		catch
		{
		}
	}

	public static class DemoData
	{
		public static List<Quest> GetSampleQuests()
		{
			return new List<Quest>
			{
				new Quest { Id = 0, Name = "Hero's Path", Description = "Main storyline.", ThemeColorHex = "#1f77b4" },
				new Quest { Id = 1, Name = "Mage's Journey", Description = "Magic side path.", ThemeColorHex = "#ff7f0e" },
				new Quest { Id = 2, Name = "Rogue's Gambit", Description = "Stealth missions.", ThemeColorHex = "#2ca02c" }
			};
		}

		public static List<QuestAction> GetSampleQuestActions()
		{
			return new List<QuestAction>
			{
				new QuestAction { Id = 1, QuestId = 0, Name = "Begin", Description = "Start your journey", PreviousQuestActionIds = Array.Empty<int>(), IsComplete = true },
				new QuestAction { Id = 2, QuestId = 0, Name = "Meet the Mentor", Description = "Find a guide", PreviousQuestActionIds = new[] { 1 }, IsComplete = true },
				new QuestAction { Id = 3, QuestId = 0, Name = "Trial of Fire", Description = "Prove yourself", PreviousQuestActionIds = new[] { 2 }, IsComplete = false },
				new QuestAction { Id = 4, QuestId = 1, Name = "Arcane Intro", Description = "Learn magic", PreviousQuestActionIds = Array.Empty<int>(), IsComplete = true },
				new QuestAction { Id = 5, QuestId = 1, Name = "Summon Familiar", Description = "Get a pet", PreviousQuestActionIds = new[] { 4 }, IsComplete = false },
				new QuestAction { Id = 6, QuestId = 2, Name = "Shadows Stir", Description = "Start sneaking", PreviousQuestActionIds = Array.Empty<int>(), IsComplete = true },
				new QuestAction { Id = 7, QuestId = 2, Name = "Heist Planning", Description = "Prepare for theft", PreviousQuestActionIds = new[] { 6 }, IsComplete = true },
				new QuestAction { Id = 8, QuestId = 2, Name = "Steal the Jewel", Description = "Execute the plan", PreviousQuestActionIds = new[] { 7 }, IsComplete = false },
				new QuestAction { Id = 9, QuestId = 0, Name = "Crossover Battle", Description = "Merge paths", PreviousQuestActionIds = new[] { 3, 5, 8 }, IsComplete = false }
			};
		}
	}
}