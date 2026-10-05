namespace PanoramicData.Blazor.Models;

/// <summary>
/// One of the agents a host offers in <see cref="PanoramicData.Blazor.PDChat"/>'s input area, for the user to choose
/// who they are talking to.
/// </summary>
/// <param name="Id">The identifier the host recognises, stored in <see cref="Interfaces.IChatService.SelectedAgentId"/>.</param>
/// <param name="Name">The name shown in the picker.</param>
/// <param name="Description">An optional description, shown as the option's tooltip.</param>
/// <param name="IconUrl">An optional icon, shown beside the picker while this agent is selected.</param>
public sealed record PDChatAgentOption(string Id, string Name, string? Description = null, string? IconUrl = null);
