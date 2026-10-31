using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.Generation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedRMCChemistryResearchSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class RMCChemistryResearchComponent : Component
{
    [DataField, AutoNetworkedField]
    public int ClearanceLevel = 1;

    [DataField, AutoNetworkedField]
    public bool ReachedXAccess;

    [DataField, AutoNetworkedField]
    public bool DnaDisintegratingDiscovered;

    [DataField, AutoNetworkedField]
    public int DnaDisintegratingCredits = 10;

    [DataField, AutoNetworkedField]
    public FixedPoint2 DnaDisintegratingPoints = 10;

    [DataField, AutoNetworkedField]
    public int Credits;

    [DataField, AutoNetworkedField]
    public List<ContractSlot> Contracts = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, List<DocumentEntry>> Documents = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, List<DocumentEntry>> Published = new();

    [DataField, AutoNetworkedField]
    public int NextDocumentId = 1;

    [DataField, AutoNetworkedField]
    public HashSet<string> IdentifiedIds = new();

    [DataField, AutoNetworkedField]
    public HashSet<string> LockedIds = new();

    [DataField, AutoNetworkedField]
    public TimeSpan NextReroll;

    [DataField, AutoNetworkedField]
    public bool PickedThisCycle;

    [DataField, AutoNetworkedField]
    public string? LastPickedContractReagent;

    [DataField, AutoNetworkedField]
    public TimeSpan NextAnnounceAt;

    [DataField, AutoNetworkedField]
    public int ResearchLevelIncreaseMultiplier = 3;

    [DataField, AutoNetworkedField]
    public int MaxClearanceLevel = 5;

    [DataField, AutoNetworkedField]
    public int XAccessClearanceTier = 6;

    [DataField, AutoNetworkedField]
    public int XAccessCost = 5;

    [DataField, AutoNetworkedField]
    public int ContractSlotCount = 3;

    [DataField, AutoNetworkedField]
    public TimeSpan ContractRerollNotPicked = TimeSpan.FromMinutes(3);

    [DataField, AutoNetworkedField]
    public TimeSpan ContractRerollPicked = TimeSpan.FromMinutes(6);

    [DataField, AutoNetworkedField]
    public TimeSpan AnnounceCooldown = TimeSpan.FromMinutes(4);

    [DataField, AutoNetworkedField]
    public int AnnounceCharacterLimit = 400;

    [DataField, AutoNetworkedField]
    public int CategoryCharacterLimit = 50;

    [DataField, AutoNetworkedField]
    public string DefaultScanCategory = "Misc.";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? NotifySound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? FaxSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/fax.ogg");
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class ContractSlot
{
    [DataField]
    public string Name = string.Empty;

    [DataField]
    public int Tier;

    [DataField]
    public string PropertyHintId = string.Empty;

    [DataField]
    public string IngredientHintId = string.Empty;

    [DataField]
    public bool Taken;

    [DataField]
    public string? ExistingReagentId;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class DocumentEntry
{
    [DataField]
    public int Id;

    [DataField]
    public NetEntity Report;

    [DataField]
    public string Title = string.Empty;

    [DataField]
    public TimeSpan Time;

    [DataField]
    public bool ManualScan;
}
