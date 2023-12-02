using Content.Shared.Damage;

namespace Content.Shared._CM14.Comtech.Barbed
{
    [RegisterComponent]
    public sealed partial class BarbedComponent : Component
    {
        [DataField(required: true)]
        public DamageSpecifier thornsDamage = default!;

        [DataField(required: true)]
        public bool IsBarbed;
    }
}
