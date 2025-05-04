using Content.Shared._RMC14.Intel.Tech;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Intel;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class IntelTechTree
{
    [DataField]
    public FixedPoint2 Points;

    [DataField]
    public FixedPoint2 TotalEarned;

    [DataField]
    public IntelObjectiveAmount Documents;

    [DataField]
    public IntelObjectiveAmount UploadData;

    [DataField]
    public IntelObjectiveAmount RetrieveItems;

    [DataField]
    public IntelObjectiveAmount Miscellaneous;

    [DataField]
    public int AnalyzeChemicals;

    [DataField]
    public int RescueSurvivors;

    [DataField]
    public int RecoverCorpses;

    [DataField]
    public bool ColonyCommunications;

    [DataField]
    public bool ColonyPower;

    [DataField]
    public int Tier;

    [DataField]
    public List<List<TechOption>> Options = new();

    [DataField]
    public Dictionary<LocId, Dictionary<NetEntity, string>> Clues = new();
}
