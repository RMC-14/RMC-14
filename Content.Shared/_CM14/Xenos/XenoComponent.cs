using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CM14.Xenos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoComponent : Component
{
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<EntProtoId> EvolvesTo = new();

    [DataField, AutoNetworkedField]
    public List<EntProtoId> ActionIds = new();

    [DataField]
    public Dictionary<EntProtoId, EntityUid> Actions = new();

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int Plasma;

    [DataField(required: true), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int MaxPlasma = 300;

    [DataField(required: true), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int PlasmaRegen;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PlasmaRegenCooldown = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextPlasmaRegenTime;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int? OriginalDrawDepth;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AcidDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string DevourContainerId = "cm_xeno_devour";

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DevourDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier RegurgitateSound = new SoundPathSpecifier("/Audio/_CM14/Xeno/alien_drool2.ogg");

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid Hive;
}
