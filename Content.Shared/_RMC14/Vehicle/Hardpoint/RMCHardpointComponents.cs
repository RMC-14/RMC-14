using System;
using System.Collections.Generic;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Tools;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCHardpointSystem), typeof(RMCHardpointSlotSystem))]
public sealed partial class RMCHardpointItemComponent : Component
{
    public const string ComponentId = "RMCHardpointItem";

    [DataField(required: true)]
    public string HardpointType = string.Empty;

    [DataField]
    public ProtoId<RMCHardpointVehicleFamilyPrototype>? VehicleFamily;

    [DataField]
    public ProtoId<RMCHardpointSlotTypePrototype>? SlotType;

    [DataField]
    public string? CompatibilityId;

    [DataField]
    public float DamageMultiplier = 1f;

    [DataField]
    public float RepairRate = 0.01f;
}


[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCHardpointSystem), typeof(RMCHardpointSlotSystem))]
public sealed partial class RMCHardpointSlotsComponent : Component
{
    [DataField]
    public ProtoId<RMCHardpointVehicleFamilyPrototype>? VehicleFamily;

    [DataField(required: true)]
    public List<RMCHardpointSlot> Slots = new();

    [DataField]
    public float FrameDamageFractionWhileIntact = 0.1f;

    [DataField]
    public ProtoId<ToolQualityPrototype> RemoveToolQuality = "Prying";

    [NonSerialized]
    public HashSet<string> PendingInserts = new();

    [NonSerialized]
    public HashSet<string> CompletingInserts = new();

    [NonSerialized]
    public HashSet<string> PendingRemovals = new();

    [NonSerialized]
    public HashSet<EntityUid> PendingInsertUsers = new();

    [NonSerialized]
    public string? LastUiError;
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class RMCHardpointSlot
{
    [DataField(required: true)]
    public string Id { get; set; } = string.Empty;

    [DataField(required: true)]
    public string HardpointType { get; set; } = string.Empty;

    [DataField]
    public ProtoId<RMCHardpointSlotTypePrototype>? SlotType { get; set; }

    [DataField]
    public string? CompatibilityId { get; set; }

    [DataField]
    public string VisualLayer { get; set; } = string.Empty;

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

    [DataField]
    public ProtoId<ToolQualityPrototype> RepairToolQuality = "Welding";

    [DataField]
    public ProtoId<ToolQualityPrototype> FrameFinishToolQuality = "Anchoring";

    [DataField]
    public float FrameWeldCapFraction = 0.75f;

    [DataField]
    public float FrameRepairEpsilon = 0.01f;

    [DataField]
    public float RepairChunkFraction = 0.05f;

    [DataField]
    public float RepairChunkMinimum = 0.01f;

    [DataField]
    public float FrameRepairChunkSeconds = 2f;

    [DataField, AutoNetworkedField]
    public bool BypassEntryOnZero;

    [NonSerialized]
    public bool Repairing;
}

[RegisterComponent]
public sealed partial class RMCHardpointDamageModifierComponent : Component
{
    [DataField("modifierSets")]
    public List<ProtoId<DamageModifierSetPrototype>> ModifierSets = new();
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

public readonly record struct RMCHardpointSlotsChangedEvent(EntityUid Vehicle);
