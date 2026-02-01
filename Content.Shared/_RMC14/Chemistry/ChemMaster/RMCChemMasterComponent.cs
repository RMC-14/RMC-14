using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Chemistry.ChemMaster;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedRMCChemMasterSystem))]
public sealed partial class RMCChemMasterComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 BottleSize = FixedPoint2.New(60);

    [DataField, AutoNetworkedField]
    public string BufferSolutionId = "buffer";

    [DataField, AutoNetworkedField]
    public string BeakerSlot = "beakerSlot";

    [DataField, AutoNetworkedField]
    public string PillBottleContainer = "rmc_pill_bottle";

    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist PillBottleWhitelist = new();

    [DataField, AutoNetworkedField]
    public int MaxPillBottles = 8;

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> SelectedBottles = new();

    [DataField, AutoNetworkedField]
    public int MaxLabelLength = 64;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi PillRsi = new(new ResPath("_RMC14/Objects/Chemistry/pills.rsi"), "pill1");

    [DataField, AutoNetworkedField]
    public ResPath PillCanisterRsi = new("_RMC14/Objects/Chemistry/pill_canister.rsi");

    [DataField, AutoNetworkedField]
    public FixedPoint2[] TransferSettings = new FixedPoint2[] { 1, 5, 10, 30, 60 };

    [DataField, AutoNetworkedField]
    public RMCChemMasterBufferMode BufferTransferMode = RMCChemMasterBufferMode.ToBeaker;

    [DataField, AutoNetworkedField]
    public int PillTypes = 22;

    [DataField, AutoNetworkedField]
    public int PillCanisterTypes = 12;

    [DataField, AutoNetworkedField]
    public int PillAmount = 16;

    [DataField, AutoNetworkedField]
    public int MaxPillAmount = 20;

    [DataField, AutoNetworkedField]
    public uint SelectedType = 1;

    [DataField, AutoNetworkedField]
    public float LinkRange = 5;

    [DataField, AutoNetworkedField]
    public EntProtoId PillProto = "CMPill";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? PillBottleInsertSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? PillBottleEjectSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagOut/revolver_magout.ogg");

    [DataField, AutoNetworkedField]
    public int MaxQuickAccessSlots = 9;
}
