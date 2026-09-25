using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines.Mutiny;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MutinyParticipantComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Rule;

    [DataField, AutoNetworkedField]
    public MutinySide Side;

    [DataField, AutoNetworkedField]
    public EntProtoId<IFFFactionComponent> IffFaction = "FactionMarine";

    [DataField]
    public SpriteSpecifier MutineerIcon =
        new SpriteSpecifier.Rsi(new ResPath("_RMC14/Interface/job_icons/Misc/mutiny.rsi"), "hudmutineer");

    [DataField]
    public SpriteSpecifier LoyalistIcon =
        new SpriteSpecifier.Rsi(new ResPath("_RMC14/Interface/job_icons/Misc/mutiny.rsi"), "hudloyalist");

    [DataField]
    public SpriteSpecifier NonCombatantIcon =
        new SpriteSpecifier.Rsi(new ResPath("_RMC14/Interface/job_icons/Misc/mutiny.rsi"), "hudnoncombat");
}
