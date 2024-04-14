using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Armor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoArmorSystem))]
public sealed partial class XenoArmorComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Armor;

    // TODO some rockets should penetrate armor
    // TODO tank/sniper flak/shotgun incendiary burst is resisted by this but penetrated
    [DataField, AutoNetworkedField]
    public int ExplosionArmor;
}
