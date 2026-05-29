using Content.Shared.Damage;

namespace Content.Shared._RMC14.Hijack;

[RegisterComponent]
public sealed partial class RMCHijackRandomDamageTargetComponent : Component
{
    [DataField]
    public bool Enabled = true;

    [DataField(required: true)]
    public RMCHijackRandomDamageCategory Category;

    [DataField]
    public DamageSpecifier? Damage;

    [DataField]
    public DamageSpecifier? BreakDamage;

    [DataField]
    public bool HijackDamageOnly;
}

public enum RMCHijackRandomDamageCategory
{
    Wall,
    Window,
    Windoor,
    Pipe,
}
