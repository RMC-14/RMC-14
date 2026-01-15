using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Armor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState()]
public sealed partial class RMCAdjustableArmorValueComponent : Component
{
    [DataField, AutoNetworkedField]
    public int MeleeArmor;

    [DataField, AutoNetworkedField]
    public int BulletArmor;

    [DataField, AutoNetworkedField]
    public int ExplosionArmor;

    [DataField, AutoNetworkedField]
    public int BioArmor;

    [DataField, AutoNetworkedField]
    public int MaxArmor = 200;
}
