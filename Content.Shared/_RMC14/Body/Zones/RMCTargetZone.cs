using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Body.Zones;

[Serializable, NetSerializable]
public enum RMCTargetZone : byte
{
    Chest = 0,
    Head,
    Eyes,
    Mouth,
    Groin,
    LeftArm,
    RightArm,
    LeftHand,
    RightHand,
    LeftLeg,
    RightLeg,
    LeftFoot,
    RightFoot,
}
