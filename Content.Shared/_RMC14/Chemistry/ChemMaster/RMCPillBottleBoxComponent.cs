using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.ChemMaster;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCPillBottleTransferComponent : Component
{
    [DataField]
    public float TimePerBottle = 0.43f;

    [DataField]
    public SoundSpecifier? UseSound = new SoundPathSpecifier("/Audio/Effects/rustle1.ogg");
}
