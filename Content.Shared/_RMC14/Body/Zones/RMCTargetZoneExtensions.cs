using Content.Shared.Body.Part;

namespace Content.Shared._RMC14.Body.Zones;

public static class RMCTargetZoneExtensions
{
    /// <summary>
    ///     The body part that takes the hit when this zone is aimed at.
    /// </summary>
    public static (BodyPartType Type, BodyPartSymmetry Symmetry) ToBodyPart(this RMCTargetZone zone)
    {
        return zone switch
        {
            RMCTargetZone.Head or RMCTargetZone.Eyes or RMCTargetZone.Mouth => (BodyPartType.Head, BodyPartSymmetry.None),
            RMCTargetZone.Chest => (BodyPartType.Torso, BodyPartSymmetry.None),
            // TODO RMC14 groin body part
            RMCTargetZone.Groin => (BodyPartType.Torso, BodyPartSymmetry.None),
            RMCTargetZone.LeftArm => (BodyPartType.Arm, BodyPartSymmetry.Left),
            RMCTargetZone.RightArm => (BodyPartType.Arm, BodyPartSymmetry.Right),
            RMCTargetZone.LeftHand => (BodyPartType.Hand, BodyPartSymmetry.Left),
            RMCTargetZone.RightHand => (BodyPartType.Hand, BodyPartSymmetry.Right),
            RMCTargetZone.LeftLeg => (BodyPartType.Leg, BodyPartSymmetry.Left),
            RMCTargetZone.RightLeg => (BodyPartType.Leg, BodyPartSymmetry.Right),
            RMCTargetZone.LeftFoot => (BodyPartType.Foot, BodyPartSymmetry.Left),
            RMCTargetZone.RightFoot => (BodyPartType.Foot, BodyPartSymmetry.Right),
            _ => (BodyPartType.Torso, BodyPartSymmetry.None),
        };
    }

    /// <summary>
    ///     Collapses eyes and mouth into the head.
    /// </summary>
    public static RMCTargetZone ToDamageZone(this RMCTargetZone zone)
    {
        return zone is RMCTargetZone.Eyes or RMCTargetZone.Mouth ? RMCTargetZone.Head : zone;
    }
}
