using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedPumpActionSystem))]
public sealed partial class PumpActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Pumped;

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("CMShotgunPump");
}
