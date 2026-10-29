using Content.Shared.FixedPoint;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship.Matrix;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DropshipMatrixSystem))]
public sealed partial class DropshipMatrixComponent : Component
{
    [DataField, AutoNetworkedField]
    public DropshipMatrixState State;

    [DataField, AutoNetworkedField]
    public DropshipMatrixUpgrade Upgrade;

    [DataField]
    public string ContainerId = "rmc_dropship_matrix_vial";

    [DataField]
    public string VialSolution = "beaker";

    [DataField]
    public FixedPoint2 RequiredVolume = 30;

    [DataField]
    public ProtoId<ToolQualityPrototype> LockQuality = "Screwing";

    [DataField]
    public SoundSpecifier? LockSound = new SoundPathSpecifier("/Audio/Items/screwdriver.ogg");
}

[Serializable, NetSerializable]
public enum DropshipMatrixState : byte
{
    Empty,
    Unlocked,
    Locked,
}

[Serializable, NetSerializable]
public enum DropshipMatrixUpgrade : byte
{
    None,
    NightVision,
    Wide,
}
