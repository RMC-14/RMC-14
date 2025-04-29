using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.Flamer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCFlamerSystem))]
public sealed partial class RMCIgniterComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;

    [DataField, AutoNetworkedField]
    public SoundPathSpecifier? Sound = new("/Audio/_RMC14/Weapons/Handling/flamer_ignition.ogg");

    [DataField, AutoNetworkedField]
    public LocId Popup = "rmc-flamer-ignite-first";

    [DataField, AutoNetworkedField]
    public LocId PopupKey = "rmc-flamer-ignite-first-with";
}
