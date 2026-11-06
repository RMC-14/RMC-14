using Content.Shared.Damage;

namespace Content.Shared._RMC14.Weapons.Melee.OmaeWaMouShindeiru;

[RegisterComponent]
public sealed partial class OmaeTargetComponent : Component
{
    public EntityUid? AudioStream;
    public EntityUid? Attacker;
    public DamageSpecifier Damage = new();
    public int NumberOfCuts;
    public bool BypassResistances;
}
