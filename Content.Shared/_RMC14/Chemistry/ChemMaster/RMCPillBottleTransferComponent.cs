using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.ChemMaster;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCPillBottleTransferComponent : Component
{
    [DataField, AutoNetworkedField]
    public float TimePerBottle = 0.3f; // time_to_empty = 3

    [DataField, AutoNetworkedField]
    public SoundSpecifier? InsertPillBottleSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");
}
