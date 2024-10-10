using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weapons.Ranged.Prediction;

[Serializable, NetSerializable]
public sealed class MaxLinearVelocityMsg(float velocity) : EntityEventArgs
{
    public float Velocity = velocity;
}
