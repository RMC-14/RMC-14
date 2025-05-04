using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedRMCFlammableSystem))]
public sealed partial class TileFirePatterComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/alien_claw_swipe.ogg");

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan Last;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public TimeSpan RemoveDuration = TimeSpan.FromSeconds(5);
}
