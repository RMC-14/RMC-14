using Content.Shared.Damage;
﻿using Robust.Shared.Serialization;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._CM14.Comtech.Barbed.Components
{
    [RegisterComponent]
    public sealed partial class BarbedComponent : Component
    {
        [DataField(required: true)]
        public DamageSpecifier ThornsDamage = default!;

        [DataField("isBarbed")]
        public bool IsBarbed = false;

        // [DataField]
        // public EntProtoId? Spawn; todo spawn a metal rod when wirecut

        [DataField("wireTime")]
        public float WireTime = 3.0f;

        [DataField("cutTime")]
        public float CutTime = 1.0f;
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

[Serializable, NetSerializable]
public sealed partial class CutBarbedDoAfterEvent : SimpleDoAfterEvent
{
}
