using System;
using System.Collections.Generic;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
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

    [DataField]
    public float HardpointDamageMultiplier = 1f;

    [DataField]
    public float FrameDamageFractionWhileIntact = 0.1f;

    [NonSerialized]
    public HashSet<string> PendingInserts = new();

    [NonSerialized]
    public HashSet<string> CompletingInserts = new();

    [NonSerialized]
    public HashSet<string> PendingRemovals = new();
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
    public float RemoveDelay { get; set; } = -1f;

    [DataField]
    public EntityWhitelist? Whitelist { get; set; }
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCHardpointIntegrityComponent : Component
{
    [DataField]
    public float MaxIntegrity = 100f;

    [DataField, AutoNetworkedField]
    public float Integrity;

    [DataField]
    public FixedPoint2 RepairFuelCost = FixedPoint2.New(5);

    [DataField]
    public SoundSpecifier? RepairSound;

    [DataField, AutoNetworkedField]
    public bool BypassEntryOnZero;

    [DataField]
    public float RepairTimePerIntegrity = 0.01f;

    [DataField]
    public float RepairTimeMin = 0.25f;

    [DataField]
    public float RepairTimeMax = 3f;

    [NonSerialized]
    public bool Repairing;
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

[Serializable, NetSerializable]
public sealed partial class RMCHardpointRemoveDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public string SlotId = string.Empty;

    public RMCHardpointRemoveDoAfterEvent()
    {
    }

    public RMCHardpointRemoveDoAfterEvent(string slotId)
    {
        SlotId = slotId;
    }

    public override DoAfterEvent Clone()
    {
        return new RMCHardpointRemoveDoAfterEvent(SlotId);
    }

    public override bool IsDuplicate(DoAfterEvent other)
    {
        return other is RMCHardpointRemoveDoAfterEvent remove
               && remove.SlotId == SlotId
               && other.User == User
               && other.Target == Target
               && other.Used == Used;
    }
}

[Serializable, NetSerializable]
public sealed partial class RMCHardpointRepairDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return new RMCHardpointRepairDoAfterEvent();
    }
}
