﻿using PanoramicData.Blazor.Models.Quests;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDQuestVisualizerPage : IAsyncDisposable
{
	private IJSObjectReference? _commonModule;

	private readonly List<Quest> _quests = DemoData.GetSampleQuests();
	private readonly List<QuestAction> _questActions = DemoData.GetSampleQuestActions();

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", JSInteropVersionHelper.CommonJsUrl);
		}
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
			return
			[
				new() { Id = 0, Name = "Hero's Path", Description = "Main storyline.", ThemeColorHex = "#1f77b4" },
				new() { Id = 1, Name = "Mage's Journey", Description = "Magic side path.", ThemeColorHex = "#ff7f0e" },
				new() { Id = 2, Name = "Rogue's Gambit", Description = "Stealth missions.", ThemeColorHex = "#2ca02c" }
			];
		}

		public static List<QuestAction> GetSampleQuestActions()
		{
			return
			[
				new() { Id = 1, QuestId = 0, Name = "Begin", Description = "Start your journey", PreviousQuestActionIds = [], IsComplete = true },
				new() { Id = 2, QuestId = 0, Name = "Meet the Mentor", Description = "Find a guide", PreviousQuestActionIds = [1], IsComplete = true },
				new() { Id = 3, QuestId = 0, Name = "Trial of Fire", Description = "Prove yourself", PreviousQuestActionIds = [2], IsComplete = false },
				new() { Id = 4, QuestId = 1, Name = "Arcane Intro", Description = "Learn magic", PreviousQuestActionIds = [], IsComplete = true },
				new() { Id = 5, QuestId = 1, Name = "Summon Familiar", Description = "Get a pet", PreviousQuestActionIds = [4], IsComplete = false },
				new() { Id = 6, QuestId = 2, Name = "Shadows Stir", Description = "Start sneaking", PreviousQuestActionIds = [], IsComplete = true },
				new() { Id = 7, QuestId = 2, Name = "Heist Planning", Description = "Prepare for theft", PreviousQuestActionIds = [6], IsComplete = true },
				new() { Id = 8, QuestId = 2, Name = "Steal the Jewel", Description = "Execute the plan", PreviousQuestActionIds = [7], IsComplete = false },
				new() { Id = 9, QuestId = 0, Name = "Crossover Battle", Description = "Merge paths", PreviousQuestActionIds = [3, 5, 8], IsComplete = false }
			];
		}
	}
}