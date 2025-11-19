using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.IFF;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ProjectileIFFComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<IFFFactionComponent>? Faction;

    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId<IFFFactionComponent>> Factions = new();

    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}
