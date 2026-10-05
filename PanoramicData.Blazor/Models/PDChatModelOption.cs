namespace PanoramicData.Blazor.Models;

/// <summary>
/// One of the models a host offers in <see cref="PanoramicData.Blazor.PDChat"/>'s input toolbar, for the user to choose
/// which model answers.
/// </summary>
/// <param name="Id">The identifier the host recognises, stored in <see cref="Interfaces.IChatService.SelectedModelId"/>.</param>
/// <param name="Name">The name shown in the picker.</param>
/// <param name="Description">An optional description, shown as the option's tooltip.</param>
public sealed record PDChatModelOption(string Id, string Name, string? Description = null);
