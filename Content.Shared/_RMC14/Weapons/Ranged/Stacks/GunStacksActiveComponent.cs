using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Weapons.Ranged.Stacks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(GunStacksSystem))]
public sealed partial class GunStacksActiveComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ExpireAt;

    [DataField, AutoNetworkedField]
    public TimeSpan StacksExpire = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public int Hits = 0;

    [DataField, AutoNetworkedField]
    public EntityUid? LastHitEntity;
}
