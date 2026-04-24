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
[Access(typeof(HardpointSystem), typeof(HardpointSlotSystem))]
public sealed partial class HardpointItemComponent : Component
{
    public const string ComponentId = "HardpointItem";

    [DataField(required: true)]
    public string HardpointType = string.Empty;

    [DataField]
    public ProtoId<HardpointVehicleFamilyPrototype>? VehicleFamily;

    [DataField]
    public ProtoId<HardpointSlotTypePrototype>? SlotType;

    [DataField]
    public string? CompatibilityId;

    [DataField]
    public float DamageMultiplier = 1f;

    [DataField]
    public float RepairRate = 0.01f;
}


[RegisterComponent, NetworkedComponent]
[Access(typeof(HardpointSystem), typeof(HardpointSlotSystem))]
public sealed partial class HardpointSlotsComponent : Component
{
    [DataField]
    public ProtoId<HardpointVehicleFamilyPrototype>? VehicleFamily;

    [DataField(required: true)]
    public List<HardpointSlot> Slots = new();

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
public sealed partial class HardpointSlot
{
    [DataField(required: true)]
    public string Id { get; set; } = string.Empty;

    [DataField(required: true)]
    public string HardpointType { get; set; } = string.Empty;

    [DataField]
    public ProtoId<HardpointSlotTypePrototype>? SlotType { get; set; }

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
public sealed partial class HardpointIntegrityComponent : Component
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
public sealed partial class HardpointDamageModifierComponent : Component
{
    [DataField("modifierSets")]
    public List<ProtoId<DamageModifierSetPrototype>> ModifierSets = new();
}

[Serializable, NetSerializable]
public sealed partial class HardpointInsertDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public string SlotId = string.Empty;

    public HardpointInsertDoAfterEvent()
    {
    }

    public HardpointInsertDoAfterEvent(string slotId)
    {
        SlotId = slotId;
    }

    public override DoAfterEvent Clone()
    {
        return new HardpointInsertDoAfterEvent(SlotId);
    }

    public override bool IsDuplicate(DoAfterEvent other)
    {
        return other is HardpointInsertDoAfterEvent hardpoint
               && hardpoint.SlotId == SlotId;
    }
}

[Serializable, NetSerializable]
public sealed partial class HardpointRemoveDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public string SlotId = string.Empty;

    public HardpointRemoveDoAfterEvent()
    {
    }

    public HardpointRemoveDoAfterEvent(string slotId)
    {
        SlotId = slotId;
    }

    public override DoAfterEvent Clone()
    {
        return new HardpointRemoveDoAfterEvent(SlotId);
    }

    public override bool IsDuplicate(DoAfterEvent other)
    {
        return other is HardpointRemoveDoAfterEvent remove
               && remove.SlotId == SlotId;
    }
}

[Serializable, NetSerializable]
public sealed partial class HardpointRepairDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return new HardpointRepairDoAfterEvent();
    }
}

public readonly record struct HardpointSlotsChangedEvent(EntityUid Vehicle);
