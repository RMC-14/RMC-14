using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Entrenching;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCFoldingBarricadeLinkingSystem))]
public sealed partial class RMCFoldingBarricadeLinkingComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Linked;

    [DataField, AutoNetworkedField]
    public bool Linkable = true;

    [DataField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillConstruction";

    [DataField]
    public int RequiredSkillLevel = 2;

    [DataField]
    public SoundSpecifier? ToggleSound = new SoundPathSpecifier("/Audio/Items/crowbar.ogg");
}

[Serializable, NetSerializable]
public enum RMCFoldingBarricadeLinkingVisualLayers : byte
{
    North,
    South,
    East,
    West,
}

[Serializable, NetSerializable]
public enum RMCFoldingBarricadeLinkingVisuals : byte
{
    North,
    South,
    East,
    West,
}

[Serializable, NetSerializable]
public enum RMCFoldingBarricadeLinkingVisualState : byte
{
    None,
    Closed,
    Open,
}
