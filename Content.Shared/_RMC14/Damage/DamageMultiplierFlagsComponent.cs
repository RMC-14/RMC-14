using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Damage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCDamageableSystem))]
public sealed partial class DamageMultiplierFlagsComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public DamageMultiplierFlag Flags;
}

[Flags]
public enum DamageMultiplierFlag : byte
{
    None = 0,
    Turf = 1 << 0,
    Breaching = 1 << 1,
    Xeno = 1 << 2,
}
