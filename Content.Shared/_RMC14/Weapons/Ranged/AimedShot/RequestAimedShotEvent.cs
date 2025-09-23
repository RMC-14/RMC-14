using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weapons.Ranged.AimedShot;

/// <summary>
///     Raised on the client to indicate it'd like to do an aimed shot.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestAimedShotEvent : EntityEventArgs
{
    public NetEntity Gun;
    public NetEntity User;
    public NetEntity Target;
}
