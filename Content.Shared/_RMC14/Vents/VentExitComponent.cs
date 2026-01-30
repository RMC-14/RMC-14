using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vents;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VentExitComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ExitSound = new SoundCollectionSpecifier("XenoVentPass");
}
