namespace Content.Shared._RMC14.Marines.Invisibility;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class MarineTurnInvisibleComponent : Component
{
    public bool Enabled;

    [DataField, AutoNetworkedField]
    public float Opacity = 1f;

    [DataField, AutoNetworkedField]
    public bool RestrictWeapons;

    [DataField]
    [AutoNetworkedField]
    public TimeSpan UncloakTime;

    /// <summary>
    /// Amount of time after uncloaking weapons remain locked
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public TimeSpan UncloakWeaponLock;
}
