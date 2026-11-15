using Content.Shared.Body.Part;

namespace Content.Shared._RMC14.Body.Zones;

public static class RMCTargetZoneExtensions
{
    /// <summary>
    ///     the body part that takes the hit when this zone is aimed at
    /// </summary>
    /// <remarks>
    ///     upstream body parts are entities identified by type and side, there is no single left arm value,
    ///     so use this when you need the body part entity itself, limb data can be keyed by the zone directly
    /// </remarks>
    public static (BodyPartType Type, BodyPartSymmetry Symmetry) ToBodyPart(this RMCTargetZone zone)
    {
        return zone switch
        {
            RMCTargetZone.Head or RMCTargetZone.Eyes or RMCTargetZone.Mouth => (BodyPartType.Head, BodyPartSymmetry.None),
            RMCTargetZone.Chest => (BodyPartType.Torso, BodyPartSymmetry.None),
            // TODO RMC14 abdomen body part
            RMCTargetZone.Abdomen => (BodyPartType.Torso, BodyPartSymmetry.None),
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
    ///     collapses eyes and mouth into the head
    /// </summary>
    public static RMCTargetZone ToDamageZone(this RMCTargetZone zone)
    {
        return zone is RMCTargetZone.Eyes or RMCTargetZone.Mouth ? RMCTargetZone.Head : zone;
    }

    // same order as cmss13's DEFENSE_ZONES_LIVING
    private static readonly RMCTargetZone[] CycleOrder =
    [
        RMCTargetZone.Head,
        RMCTargetZone.Eyes,
        RMCTargetZone.Mouth,
        RMCTargetZone.Chest,
        RMCTargetZone.Abdomen,
        RMCTargetZone.LeftArm,
        RMCTargetZone.LeftHand,
        RMCTargetZone.RightArm,
        RMCTargetZone.RightHand,
        RMCTargetZone.LeftLeg,
        RMCTargetZone.LeftFoot,
        RMCTargetZone.RightLeg,
        RMCTargetZone.RightFoot,
    ];

    public static RMCTargetZone Next(this RMCTargetZone zone)
    {
        var index = Array.IndexOf(CycleOrder, zone);
        return CycleOrder[(index + 1) % CycleOrder.Length];
    }

    public static RMCTargetZone Previous(this RMCTargetZone zone)
    {
        var index = Array.IndexOf(CycleOrder, zone);
        return CycleOrder[(index - 1 + CycleOrder.Length) % CycleOrder.Length];
    }

    /// <summary>
    ///     head, then eyes, then mouth, then back to head
    /// </summary>
    public static RMCTargetZone CycleHead(this RMCTargetZone zone)
    {
        return zone switch
        {
            RMCTargetZone.Head => RMCTargetZone.Eyes,
            RMCTargetZone.Eyes => RMCTargetZone.Mouth,
            _ => RMCTargetZone.Head,
        };
    }

    /// <summary>
    ///     selects the limb, or its hand or foot if the limb is already selected
    /// </summary>
    public static RMCTargetZone CycleLimb(this RMCTargetZone zone, RMCTargetZone limb, RMCTargetZone extremity)
    {
        return zone == limb ? extremity : limb;
    }
}
