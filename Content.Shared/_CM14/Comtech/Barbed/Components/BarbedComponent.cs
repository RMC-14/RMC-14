using Content.Shared._CM14.Comtech.Barbed;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
﻿using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Comtech.Barbed.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    [Access(typeof(BarbedSystem))]
    public sealed partial class BarbedComponent : Component
    {
        [DataField(required: true)]
        public DamageSpecifier ThornsDamage = default!;

        [DataField]
        public bool IsBarbed = false;

        // [DataField]
        // public EntProtoId? Spawn; todo spawn a metal rod when wirecut

        [DataField]
        public float WireTime = 2.0f;

        [DataField]
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
