using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.Recoil;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(GunToggleableRecoilSystem))]
public sealed partial class GunToggleableRecoilComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField, AutoNetworkedField]
    public float BatteryDrain = 1.25f;

    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionToggleRecoil";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");
}
