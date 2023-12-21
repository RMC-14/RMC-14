using Content.Shared.Damage;
using Robust.Shared.Containers;
﻿using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared._CM14.Comtech.Barbed.Components
{
    [RegisterComponent]
    public sealed partial class BarbedComponent : Component
    {
        [DataField(required: true)]
        public DamageSpecifier ThornsDamage = default!;

        [ViewVariables]
        public ContainerSlot BarbedSlot = default!;

        [DataField("wireTime")]
        public float WireTime = 3.0f;
    }

    [NetSerializable, Serializable]
    public enum BarbedWireVisuals : byte
    {
        Wired,
    }
}

[Serializable, NetSerializable]
public sealed partial class BarbedDoAfterEvent : SimpleDoAfterEvent
{
}
