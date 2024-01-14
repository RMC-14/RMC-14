using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMPumpActionSystem))]
public sealed partial class CMPumpActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Pumped;

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("CMShotgunPump");
}
