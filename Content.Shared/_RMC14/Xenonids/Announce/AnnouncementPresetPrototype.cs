using Content.Shared._RMC14.Announce;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Shared._RMC14.Announce;

[Prototype]
[DataDefinition, NetSerializable, Serializable]
public sealed partial class AnnouncementPresetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name { get; private set; } = string.Empty;

    [DataField]
    public string Description { get; private set; } = string.Empty;

    [DataField]
    public AnnouncementTarget Target { get; private set; } = AnnouncementTarget.All;

    [DataField]
    public AnnouncementStyle Style { get; private set; } = new();

    [DataField]
    public SoundSpecifier? Sound { get; private set; }

    [DataField]
    public float SoundVolume { get; private set; } = 1.0f;

    [DataField]
    public float Priority { get; private set; } = 5.0f;

    [DataField]
    public bool CanInterrupt { get; private set; } = false;

    [DataField]
    public bool CanBeInterrupted { get; private set; } = true;

    [DataField]
    public List<string> Aliases { get; private set; } = new();

    [DataField]
    public string? StylizedVariant { get; private set; }

    [DataField]
    public string? DefaultVariant { get; private set; }

    [DataField]
    public string? SimplifiedVariant { get; private set; }

    [DataField]
    public string? DecalRsi { get; private set; }

    [DataField]
    public string? DecalState { get; private set; }

    [DataField]
    public AnnouncementDecalPlacement? DecalPlacement { get; private set; }

    [DataField]
    public bool ShowSprite { get; private set; } = true;

    [DataField]
    public float DecalScale { get; private set; } = 4f;

    [DataField]
    public float DecalAlpha { get; private set; } = 1f;

    [DataField]
    public Vector2 DecalOffset { get; private set; } = Vector2.Zero;

    [DataField]
    public Vector2 TextOffset { get; private set; } = Vector2.Zero;

    [DataField]
    public bool VisibleInSettings { get; private set; } = true;
}
