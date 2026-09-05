using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Linq;

namespace Content.Shared._RMC14.Medical.Surgery.Tools;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class RMCSurgeryToolComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> SkillType = "RMCSkillSurgery";

    [DataField, AutoNetworkedField]
    public int Skill = 1;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? StartSound;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? EndSound;

    [DataField, AutoNetworkedField]
    public List<RMCSurgeryToolTypeEntry> ToolTypes = new();

    [DataField, AutoNetworkedField]
    public bool RequiresHotCautery = false;

    [DataField, AutoNetworkedField]
    public bool OpenSurgeryMenu = false;

    public bool TryGetBestTypeForStep(EntProtoId step, IReadOnlyCollection<RMCSurgeryToolKind> requiredKinds, out RMCSurgeryToolTypeResolved resolved)
    {
        RMCSurgeryToolTypeResolved? best = null;

        foreach (var toolType in ToolTypes)
        {
            if (requiredKinds.Count > 0 && !requiredKinds.Contains(toolType.Kind))
                continue;

            var quality = toolType.Quality;
            var speedMultiplier = 1f;

            foreach (var stepOverride in toolType.StepOverrides ?? Enumerable.Empty<RMCSurgeryStepToolOverride>())
            {
                if (stepOverride.Step != step)
                    continue;

                if (stepOverride.Quality is { } overrideQuality)
                    quality = overrideQuality;

                speedMultiplier *= stepOverride.SpeedMultiplier;
                break;
            }

            var candidate = new RMCSurgeryToolTypeResolved(toolType.Kind, quality, speedMultiplier);
            if (best == null || candidate.Quality > best.Value.Quality)
                best = candidate;
        }

        if (best is not { } bestType)
        {
            resolved = default;
            return false;
        }

        resolved = bestType;
        return true;
    }
}

[Serializable, NetSerializable]
public enum RMCSurgeryToolKind : byte
{
    Retractor,
    Hemostat,
    Cautery,
    Drill,
    Scalpel,
    BoneSaw,
    BoneGel,
    ScalpelManager,
    LaserScalpel,
    PictSystem,
}

[Serializable, NetSerializable]
public enum RMCSurgeryToolQuality : byte
{
    Awful,
    BadSubstitute,
    Substitute,
    Suboptimal,
    Ideal,
}

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct RMCSurgeryToolTypeEntry(
    [property: DataField(required: true)] RMCSurgeryToolKind Kind,
    [property: DataField(required: true)] RMCSurgeryToolQuality Quality,
    [property: DataField] List<RMCSurgeryStepToolOverride>? StepOverrides
)
{
    public RMCSurgeryToolTypeEntry(RMCSurgeryToolKind kind, RMCSurgeryToolQuality quality) : this(kind, quality, new List<RMCSurgeryStepToolOverride>())
    {
    }
}

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct RMCSurgeryStepToolOverride(
    [property: DataField(required: true)] EntProtoId Step,
    [property: DataField] RMCSurgeryToolQuality? Quality = null,
    [property: DataField] float SpeedMultiplier = 1f,
    [property: DataField] float? ClampBleedersChance = null
);

public readonly record struct RMCSurgeryToolTypeResolved(
    RMCSurgeryToolKind Kind,
    RMCSurgeryToolQuality Quality,
    float SpeedMultiplier
);