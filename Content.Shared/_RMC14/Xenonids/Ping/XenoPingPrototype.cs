using Content.Shared._RMC14.Ping;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using System.Collections.Generic;

namespace Content.Shared._RMC14.Xenonids.Ping;

[RegisterComponent]
public sealed partial class XenoPingDataComponent : Component, RMCPingDataComponent
{
    [DataField(required: true)]
    public string Name { get; set; } = string.Empty;

    [DataField]
    public string Description { get; set; } = string.Empty;

    [DataField(required: true)]
    public string ChatMessage { get; set; } = string.Empty;

    [DataField(required: true)]
    public string PopupMessage { get; set; } = string.Empty;

    [DataField]
    public SoundSpecifier? Sound { get; set; }

    [DataField]
    public int Priority { get; set; } = 0;

    [DataField]
    public bool IsConstruction { get; set; } = false;

    [DataField]
    public HashSet<string> Categories { get; set; } = new();

    IReadOnlyCollection<string> RMCPingDataComponent.Categories => Categories;
}
