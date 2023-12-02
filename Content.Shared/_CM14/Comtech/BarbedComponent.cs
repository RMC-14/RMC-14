using Content.Shared.Damage;

namespace Content.Server._CM14.Comtech
{
    [RegisterComponent]
    public sealed partial class BarbedComponent : Component
    {
    [DataField(required: true)]
    public DamageSpecifier ThornsDamage = default!;
    }
}
