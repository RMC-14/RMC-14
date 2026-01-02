using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Weapons.Ranged.Chamber;

// You might be wondering why this exists if upstream has a ChamberMagazineAmmoProviderComponent
// The answer is because its a buggy badly implemented pile of shit that does a lot more than
// "add a chamber to this gun"
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(RMCGunChamberSystem))]
public sealed partial class RMCGunChamberComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public string ContainerId = "rmc_gun_chamber";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/Guns/Cock/gun_cocked2.ogg", AudioParams.Default.WithMaxDistance(3));

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? LastChamberedAt;

    [DataField, AutoNetworkedField]
    public TimeSpan ChamberCooldown = TimeSpan.FromSeconds(1);
}
