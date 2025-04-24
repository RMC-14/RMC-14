using Robust.Shared.Serialization;

namespace Content.Shared.NPC.Prototypes;


[Serializable, NetSerializable]
public sealed partial class RefreshFactionDataEvent : EntityEventArgs
{
    public Dictionary<string, FactionData>? Factions;

    public RefreshFactionDataEvent(Dictionary<string, FactionData>? factions = null)
    {
        Factions = factions;
    }
};
