using Content.Shared._RMC14.Marines.Access;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.IFF;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(GunIFFSystem), typeof(IdModificationConsoleSystem))]
public sealed partial class ItemIFFComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId<IFFFactionComponent>> Factions = new();
}
