using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.IFF;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(GunIFFSystem))]
public sealed partial class ProjectileIFFComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId<IFFFactionComponent>? Faction;

    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId<IFFFactionComponent>> Factions = new();

    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}
