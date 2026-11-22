using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.DualTube;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(RMCDualTubeSystem))]
public sealed partial class RMCDualTubeComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "rmc_dual_tube";

    [ViewVariables]
    public Container Container = default!;

    [DataField, AutoNetworkedField]
    public List<EntityUid> Entities = new();

    [DataField, AutoNetworkedField]
    public int UnspawnedCount;

    [DataField, AutoNetworkedField]
    public bool SecondTubeActive;

    [DataField, AutoNetworkedField]
    public bool ChamberSwap;

    [DataField, AutoNetworkedField]
    public EntProtoId ChamberSwapActionId = "RMCActionToggleChamberSwap";

    [DataField, AutoNetworkedField]
    public EntityUid? ChamberSwapAction;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SwitchSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/switch.ogg", AudioParams.Default.WithVolume(-5));

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ChamberSwapSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/Handling/gun_burst_toggle.ogg", AudioParams.Default.WithVolume(-5));
}
