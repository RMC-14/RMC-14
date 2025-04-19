using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunIDLockComponent : Component
{

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid User = EntityUid.Invalid;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Locked = true;

    [DataField, AutoNetworkedField]
    public EntProtoId ActionID = "RMCActionToggleIDLock";

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi LockedIcon = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Actions/id_lock_actions.rsi"), "id_lock_locked");

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi UnlockedIcon = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Actions/id_lock_actions.rsi"), "id_lock_unlocked");

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");

}
