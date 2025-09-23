using Content.Shared._RMC14.Announce;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using System.Numerics;

namespace Content.Server._RMC14.Announce.Core;

public sealed class AnnouncementBuilder
{
    private readonly GeneralAnnounceSystem _announceSystem;

    private string _message = string.Empty;
    private string? _preset;
    private AnnouncementTarget _target = AnnouncementTarget.All;
    private EntityUid? _speaker;
    private EntityUid? _source;
    private EntityUid? _targetEntity;
    private AnnouncementStyle? _styleOverride;
    private SoundSpecifier? _soundOverride;
    private float? _volumeOverride;
    private float? _priorityOverride;
    private bool? _canInterrupt;
    private bool? _canBeInterrupted;
    private bool _showSprite = true;
    private float _spriteScale = 1.0f;
    private Vector2? _spriteOffset;
    private string? _speakerNameOverride;

    public AnnouncementBuilder(GeneralAnnounceSystem announceSystem)
    {
        _announceSystem = announceSystem;
    }

    public AnnouncementBuilder WithMessage(string message)
    {
        _message = message;
        return this;
    }

    public AnnouncementBuilder WithPreset(string preset)
    {
        _preset = preset;
        return this;
    }

    public AnnouncementBuilder WithTarget(AnnouncementTarget target)
    {
        _target = target;
        return this;
    }

    public AnnouncementBuilder WithSpeaker(EntityUid speaker)
    {
        _speaker = speaker;
        return this;
    }

    public AnnouncementBuilder WithSource(EntityUid source)
    {
        _source = source;
        return this;
    }

    public AnnouncementBuilder WithTargetEntity(EntityUid targetEntity)
    {
        _targetEntity = targetEntity;
        return this;
    }

    public AnnouncementBuilder WithStyleOverride(AnnouncementStyle style)
    {
        _styleOverride = style;
        return this;
    }

    public AnnouncementBuilder WithSoundOverride(SoundSpecifier sound)
    {
        _soundOverride = sound;
        return this;
    }

    public AnnouncementBuilder WithVolumeOverride(float volume)
    {
        _volumeOverride = volume;
        return this;
    }

    public AnnouncementBuilder WithPriorityOverride(float priority)
    {
        _priorityOverride = priority;
        return this;
    }

    public AnnouncementBuilder WithCanInterrupt(bool canInterrupt)
    {
        _canInterrupt = canInterrupt;
        return this;
    }

    public AnnouncementBuilder WithCanBeInterrupted(bool canBeInterrupted)
    {
        _canBeInterrupted = canBeInterrupted;
        return this;
    }

    public AnnouncementBuilder WithShowSprite(bool showSprite)
    {
        _showSprite = showSprite;
        return this;
    }

    public AnnouncementBuilder WithSpriteScale(float scale)
    {
        _spriteScale = scale;
        return this;
    }

    public AnnouncementBuilder WithSpriteOffset(Vector2 offset)
    {
        _spriteOffset = offset;
        return this;
    }

    public AnnouncementBuilder WithSpeakerNameOverride(string speakerName)
    {
        _speakerNameOverride = speakerName;
        return this;
    }

    public void Send()
    {
        var request = new AnnouncementRequest
        {
            Message = _message,
            Preset = _preset,
            Target = _target,
            Speaker = _speaker,
            Source = _source,
            TargetEntity = _targetEntity,
            StyleOverride = _styleOverride,
            SoundOverride = _soundOverride,
            VolumeOverride = _volumeOverride,
            PriorityOverride = _priorityOverride,
            CanInterrupt = _canInterrupt,
            CanBeInterrupted = _canBeInterrupted,
            ShowSprite = _showSprite,
            SpriteScale = _spriteScale,
            SpriteOffset = _spriteOffset,
            SpeakerNameOverride = _speakerNameOverride
        };

        _announceSystem.AnnounceAdvanced(request);
    }

    public static AnnouncementBuilder Create(GeneralAnnounceSystem announceSystem)
    {
        return new AnnouncementBuilder(announceSystem);
    }
}
