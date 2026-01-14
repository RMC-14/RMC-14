using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedHefaSwordSplosionSystem))]
public sealed partial class HefaSwordOnHitTriggerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Primed;

    [DataField]
    public SoundSpecifier? PrimeSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");

    [DataField]
    public LocId PrimedPopup = "rmc-hefa-sword-primed";

    [DataField]
    public LocId DeprimedPopup = "rmc-hefa-sword-deprimed";
}
