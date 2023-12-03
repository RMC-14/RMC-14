using Content.Shared.Damage;
using Robust.Shared.Containers;
﻿using Robust.Shared.Serialization;

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

    [NetSerializable, Serializable]
    public enum BarbedWireVisuals : byte
    {
        Wired,
    }
}
