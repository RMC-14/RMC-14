using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.IFF;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(GunIFFSystem))]
public sealed partial class UserIFFComponent : Component
{
    // NOTE: Only set the Faction for things that can't hold a ID.
    // NOTE: Add ItemIFF to the person's ID instead if you want to set a faction.
    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId<IFFFactionComponent>> Factions = new();
}
