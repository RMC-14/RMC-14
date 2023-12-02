using Content.Shared.Damage;
using Robust.Shared.Containers;

namespace Content.Shared._CM14.Comtech.Barbed.Components
{
    [RegisterComponent]
    public sealed partial class BarbedComponent : Component
    {
        [DataField(required: true)]
        public DamageSpecifier thornsDamage = default!;

        [ViewVariables]
        public ContainerSlot BarbedSlot = default!;
    }
}
