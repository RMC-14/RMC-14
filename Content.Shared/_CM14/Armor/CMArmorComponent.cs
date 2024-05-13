using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Armor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMArmorSystem))]
public sealed partial class CMArmorComponent : Component
{
    // TODO CM14 other types of armor
    [DataField, AutoNetworkedField]
    public int Armor;

    // TODO CM14 some rockets should penetrate armor
    // TODO CM14 tank/sniper flak/shotgun incendiary burst is resisted by this but penetrated
    [DataField, AutoNetworkedField]
    public int ExplosionArmor;
}
