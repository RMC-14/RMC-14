using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Acid;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoAcidHoleSystem))]
public sealed partial class XenoAcidHoleWallComponent : Component
{
    [DataField]
    public EntProtoId HolePrototype = "RMCAcidHole";

    [DataField]
    public float DamageNearCapRatio = 0.9f;

    [DataField]
    public SoundSpecifier? HoleCreatedSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/acid_impact1.ogg");

    [DataField]
    public SoundSpecifier? HoleExpandSound = new SoundCollectionSpecifier("XenoPry");

    [AutoNetworkedField]
    public EntityUid? Hole;

    public Direction? PendingDirection;
}
