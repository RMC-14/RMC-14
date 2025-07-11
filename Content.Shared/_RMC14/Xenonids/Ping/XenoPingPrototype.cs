using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using System.Collections.Generic;

namespace Content.Shared._RMC14.Xenonids.Ping;

[RegisterComponent]
public sealed partial class XenoPingDataComponent : Component
{
    [DataField("name", required: true)]
    public string Name { get; set; } = string.Empty;

    [DataField("description")]
    public string Description { get; set; } = string.Empty;

    [DataField("chatMessage", required: true)]
    public string ChatMessage { get; set; } = string.Empty;

    [DataField("popupMessage", required: true)]
    public string PopupMessage { get; set; } = string.Empty;

    [DataField("sound")]
    public SoundSpecifier? Sound { get; set; }

    [DataField("priority")]
    public int Priority { get; set; } = 0;

    [DataField("isConstruction")]
    public bool IsConstruction { get; set; } = false;

    [DataField("categories")]
    public HashSet<string> Categories { get; set; } = new();
}
