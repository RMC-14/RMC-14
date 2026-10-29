using Content.Shared._RMC14.Hallucination.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Hallucination;

[RegisterComponent]
public sealed partial class RMCHallucinationStatusEffectComponent : Component
{
    [DataField]
    public List<RMCHallucinationEventWeight> Events = new()
    {
        new("RMCHallucinationEventFakeHud", 8),
        new("RMCHallucinationEventFakeItem", 6),
        new("RMCHallucinationEventDangerFlash", 8),
        new("RMCHallucinationEventFakeBody", 5),
        new("RMCHallucinationEventFakeRadio", 5),
        new("RMCHallucinationEventFakeAnnouncement", 2),
        new("RMCHallucinationEventBodyMessage", 5),
        new("RMCHallucinationEventMotionBlips", 4),
        new("RMCHallucinationEventXenoSighting", 6),
        new("RMCHallucinationEventFakeGrenade", 4),
        new("RMCHallucinationEventFakeParasite", 4),
        new("RMCHallucinationEventXenoDisguise", 4),
        new("RMCHallucinationEventFakeInfection", 4),
        new("RMCHallucinationEventFakeQueen", 2),
        new("RMCHallucinationEventFakeOrbitalStrike", 2),
        new("RMCHallucinationEventFakeMortar", 3),
        new("RMCHallucinationEventFakeCas", 2),
        new("RMCHallucinationEventLightFlicker", 12),
        new("RMCHallucinationEventCallName", 4),
        new("RMCHallucinationEventDoorPry", 3),
        new("RMCHallucinationEventFakeChestburst", 3),
        new("RMCHallucinationEventEdgeLurker", 4),
        new("RMCHallucinationEventFakeWeeds", 3),
    };

    [DataField]
    public float FakeAttackChance = 0.03f;

    [DataField]
    public float FakeAttackMinRemaining;
}

[DataDefinition]
public sealed partial class RMCHallucinationEventWeight
{
    [DataField(required: true)]
    public EntProtoId<RMCHallucinationEventComponent> Event;

    [DataField]
    public int Weight = 1;

    [DataField]
    public float MinRemaining;

    public RMCHallucinationEventWeight()
    {
    }

    public RMCHallucinationEventWeight(EntProtoId<RMCHallucinationEventComponent> ev, int weight, float minRemaining = 0)
    {
        Event = ev;
        Weight = weight;
        MinRemaining = minRemaining;
    }
}
