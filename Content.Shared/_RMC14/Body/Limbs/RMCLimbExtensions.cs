using Content.Shared._RMC14.Body.Zones;
using Content.Shared.Body.Part;

namespace Content.Shared._RMC14.Body.Limbs;

public static class RMCLimbExtensions
{
    /// <summary>
    ///     the zone a body part belongs to, the reverse of ToBodyPart
    /// </summary>
    public static RMCTargetZone? ToZone(this BodyPartType type, BodyPartSymmetry symmetry)
    {
        return (type, symmetry) switch
        {
            (BodyPartType.Head, _) => RMCTargetZone.Head,
            // TODO RMC14 abdomen body part
            (BodyPartType.Torso, _) => RMCTargetZone.Chest,
            (BodyPartType.Arm, BodyPartSymmetry.Left) => RMCTargetZone.LeftArm,
            (BodyPartType.Arm, BodyPartSymmetry.Right) => RMCTargetZone.RightArm,
            (BodyPartType.Hand, BodyPartSymmetry.Left) => RMCTargetZone.LeftHand,
            (BodyPartType.Hand, BodyPartSymmetry.Right) => RMCTargetZone.RightHand,
            (BodyPartType.Leg, BodyPartSymmetry.Left) => RMCTargetZone.LeftLeg,
            (BodyPartType.Leg, BodyPartSymmetry.Right) => RMCTargetZone.RightLeg,
            (BodyPartType.Foot, BodyPartSymmetry.Left) => RMCTargetZone.LeftFoot,
            (BodyPartType.Foot, BodyPartSymmetry.Right) => RMCTargetZone.RightFoot,
            _ => null,
        };
    }
}
