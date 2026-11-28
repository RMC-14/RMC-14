using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Requisitions.Components;

[Serializable, NetSerializable]
public enum RMCBlackMarketTradebandVisuals
{
    Active,
}

[Serializable, NetSerializable]
public enum RMCBlackMarketTradebandLayers
{
    Base,
}

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRequisitionsSystem))]
public sealed partial class RMCBlackMarketTradebandDeviceComponent : Component
{
    [DataField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillPolice";

    [DataField]
    public int SkillLevel = 2;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(15);

    [DataField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/lockenable.ogg");

    [DataField]
    public SoundSpecifier FinishSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/ping.ogg");
}
