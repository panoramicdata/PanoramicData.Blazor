using System.Text.Json;
using System.Text.Json.Serialization;

namespace PanoramicData.Blazor;

/// <summary>
/// Displays a status icon that, when clicked, opens a cascading pop-over showing
/// the full health hierarchy described by the Node parameter.
/// Supports fully custom statuses via <see cref="StatusType.Custom"/>.
/// </summary>
public partial class PDStatusCascade : IAsyncDisposable
{
    private static int _idSequence;
    private readonly string _triggerId = $"pdsc-{++_idSequence}";
    private IJSObjectReference? _module;
    private DotNetObjectReference<PDStatusCascade>? _dotNetRef;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new StatusTypeJsonConverter() }
    };

    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    /// <summary>Gets or sets the status tree root node.</summary>
    [Parameter] public PDStatusCascadeNode? Node { get; set; }

    /// <summary>Gets or sets an optional text label rendered beside the trigger icon.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Gets or sets the CSS font-size for the trigger icon (e.g. "1rem", "16px"). When null (default), inherits naturally.</summary>
    [Parameter] public string? TriggerIconSize { get; set; }

    /// <summary>Gets or sets the tooltip text shown when hovering over the trigger icon.</summary>
    [Parameter] public string TriggerTitle { get; set; } = "Click to view status";

    /// <summary>
    /// Optional callback invoked just before a node's popup is shown (including drill-downs).
    /// Receives the node about to be expanded; return an updated node to replace it, or null to
    /// leave it unchanged. When not set the component behaves as a static tree.
    /// </summary>
    [Parameter] public Func<PDStatusCascadeNode, Task<PDStatusCascadeNode?>>? OnBeforeExpand { get; set; }

    private string GetIconClass() => Node?.Status?.DefaultIconClass ?? StatusType.Gray.DefaultIconClass;
    private string GetColorClass() => Node?.Status?.DefaultColorClass ?? StatusType.Gray.DefaultColorClass;

    /// <summary>
    /// Called from JavaScript when a node is about to be expanded.
    /// The nodePath is a dot-separated index path ("" = root, "0" = first child, "1.2" = second child's third child).
    /// Returns updated node JSON, or null if unchanged.
    /// </summary>
    [JSInvokable]
    public async Task<string?> ExpandNodeAsync(string nodePath)
    {
        if (OnBeforeExpand is null || Node is null)
        {
            return null;
        }

        var target = ResolveNode(Node, nodePath);
        if (target is null)
        {
            return null;
        }

        var updated = await OnBeforeExpand(target).ConfigureAwait(true);
        if (updated is null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(nodePath))
        {
            Node.Status = updated.Status;
            Node.Title = updated.Title;
            Node.Summary = updated.Summary;
            Node.Detail = updated.Detail;
            Node.Children = updated.Children;
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
        }
        else
        {
            PatchNode(Node, nodePath, updated);
        }

        return JsonSerializer.Serialize(updated, _jsonOptions);
    }

    private static PDStatusCascadeNode? ResolveNode(PDStatusCascadeNode root, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return root;
        }

        var node = root;
        foreach (var segment in path.Split('.'))
        {
            if (!int.TryParse(segment, out var idx) || idx < 0 || idx >= node.Children.Count)
            {
                return null;
            }

            node = node.Children[idx];
        }

        return node;
    }

    private static void PatchNode(PDStatusCascadeNode root, string path, PDStatusCascadeNode replacement)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var segments = path.Split('.');
        var node = root;
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (!int.TryParse(segments[i], out var idx) || idx < 0 || idx >= node.Children.Count)
            {
                return;
            }

            node = node.Children[idx];
        }

        if (int.TryParse(segments[^1], out var lastIdx) && lastIdx >= 0 && lastIdx < node.Children.Count)
        {
            node.Children[lastIdx] = replacement;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Node is not null)
        {
            try
            {
                _module = await JSRuntime.InvokeAsync<IJSObjectReference>(
                    "import", "./_content/PanoramicData.Blazor/PDStatusCascade.razor.js");

                var nodeJson = JsonSerializer.Serialize(Node, _jsonOptions);

                DotNetObjectReference<PDStatusCascade>? dotNetRef = null;
                if (OnBeforeExpand is not null)
                {
                    _dotNetRef = DotNetObjectReference.Create(this);
                    dotNetRef = _dotNetRef;
                }

                await _module.InvokeVoidAsync("init", _triggerId, nodeJson, dotNetRef);
            }
            catch
            {
                // Fast page switching may dispose the module before init completes.
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("dispose", _triggerId);
                await _module.DisposeAsync();
            }
            catch
            {
                // Ignore disposal errors.
            }
        }

        _dotNetRef?.Dispose();
    }
}

/// <summary>
/// Serialises a <see cref="StatusType"/> as a plain object with name/iconClass/colorClass
/// so the JS engine can resolve icon and colour without a dictionary lookup.
/// </summary>
internal sealed class StatusTypeJsonConverter : JsonConverter<StatusType>
{
    public override StatusType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Deserialisation is not needed for the component (one-way to JS), but handle gracefully.
        using var doc = JsonDocument.ParseValue(ref reader);
        var name = doc.RootElement.GetProperty("name").GetString() ?? "gray";
        var icon = doc.RootElement.GetProperty("iconClass").GetString() ?? StatusType.Gray.DefaultIconClass;
        var color = doc.RootElement.GetProperty("colorClass").GetString() ?? StatusType.Gray.DefaultColorClass;
        return StatusType.Custom(name, icon, color);
    }

    public override void Write(Utf8JsonWriter writer, StatusType value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("name", value.Name);
        writer.WriteString("iconClass", value.DefaultIconClass);
        writer.WriteString("colorClass", value.DefaultColorClass);
        writer.WriteEndObject();
    }
}
