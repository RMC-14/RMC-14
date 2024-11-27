using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedPumpActionSystem))]
public sealed partial class PumpActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Pumped;

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("CMShotgunPump");

    [DataField, AutoNetworkedField]
    public LocId Examine = "cm-gun-pump-examine";

    [DataField, AutoNetworkedField]
    public LocId Popup = "cm-gun-pump-first";

    [DataField, AutoNetworkedField]
    public LocId PopupKey = "cm-gun-pump-first-with";

    [DataField, AutoNetworkedField]
    public bool Once;

    [DataField, AutoNetworkedField]
    public string ContainerId = "gun_magazine";
}
