using Content.Shared.Whitelist;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Clothing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCClothingSystem))]
public sealed partial class ClothingFactionLockedComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<NpcFactionPrototype>> Whitelist = new();

    [DataField, AutoNetworkedField]
    public string DenyReason = "rmc-jumpsuit-not-faction";
}
