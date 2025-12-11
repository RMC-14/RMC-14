using System;
using System.Collections.Generic;
using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCHardpointSystem))]
public sealed partial class RMCHardpointItemComponent : Component
{
    public const string ComponentId = "RMCHardpointItem";

    [DataField(required: true)]
    public string HardpointType = string.Empty;
}


[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCHardpointSystem))]
public sealed partial class RMCHardpointSlotsComponent : Component
{
    [DataField(required: true)]
    public List<RMCHardpointSlot> Slots = new();

    [NonSerialized]
    public HashSet<string> PendingInserts = new();

    [NonSerialized]
    public HashSet<string> CompletingInserts = new();
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class RMCHardpointSlot
{
    [DataField(required: true)]
    public string Id { get; set; } = string.Empty;

    [DataField(required: true)]
    public string HardpointType { get; set; } = string.Empty;

    [DataField]
    public bool Required { get; set; } = true;

    [DataField]
    public float InsertDelay { get; set; } = 1f;

    [DataField]
    public EntityWhitelist? Whitelist { get; set; }
}

[Serializable, NetSerializable]
public sealed partial class RMCHardpointInsertDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public string SlotId = string.Empty;

    public RMCHardpointInsertDoAfterEvent()
    {
    }

    public RMCHardpointInsertDoAfterEvent(string slotId)
    {
        SlotId = slotId;
    }

    public override DoAfterEvent Clone()
    {
        return new RMCHardpointInsertDoAfterEvent(SlotId);
    }

    public override bool IsDuplicate(DoAfterEvent other)
    {
        return other is RMCHardpointInsertDoAfterEvent hardpoint
               && hardpoint.SlotId == SlotId
               && other.User == User
               && other.Target == Target
               && other.Used == Used;
    }
}
