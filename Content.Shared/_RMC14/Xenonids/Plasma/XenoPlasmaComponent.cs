using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Plasma;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoPlasmaSystem))]
public sealed partial class XenoPlasmaComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Plasma;

    [DataField(required: true), AutoNetworkedField]
    public int MaxPlasma = 300;

    [DataField, AutoNetworkedField]
    public TimeSpan PlasmaTransferDelay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public SoundSpecifier PlasmaTransferSound = new SoundCollectionSpecifier("XenoDrool");

    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 PlasmaRegenOnWeeds;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaRegenOffWeeds = 0.05;

    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> Alert = "XenoPlasma";
}
