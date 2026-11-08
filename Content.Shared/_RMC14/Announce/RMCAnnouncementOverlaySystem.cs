using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Announce.Animations;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared.Access.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Announce;

public sealed class RMCAnnouncementOverlaySystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly SharedRankSystem _rank = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly List<ICommonSession> _expired = new();
    private TimeSpan _portraitGrace;

    public override void Initialize()
    {
        Subs.CVar(_config, RMCCVars.RMCAnnouncementPortraitGrace, v => _portraitGrace = TimeSpan.FromSeconds(v), true);
    }

    public void Show(RMCAnnouncementOverlay? overlay, Filter filter)
    {
        if (overlay is not { } announcement || _net.IsClient || filter.Count == 0)
            return;

        if (!TryGetPreset(announcement.Preset, out var preset))
        {
            Log.Error($"Tried to show announcement overlay with invalid preset {announcement.Preset}");
            return;
        }

        var lines = FormattedMessage.RemoveMarkupPermissive(announcement.Text)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var text = string.Join('\n', lines);
        if (lines.Length == 0 || lines.Length > preset.MaxLines || text.Length > preset.MaxLength)
            return;

        var speakerName = announcement.SpeakerName;
        string? speakerJob = null;
        NetEntity? portrait = null;
        if (announcement.Speaker is { } speaker && !TerminatingOrDeleted(speaker))
        {
            speakerName ??= _rank.GetSpeakerRankName(speaker) ?? Name(speaker);
            if (_idCard.TryFindIdCard(speaker, out var idCard) && !string.IsNullOrWhiteSpace(idCard.Comp.LocalizedJobTitle))
                speakerJob = idCard.Comp.LocalizedJobTitle;

            if (preset.Template == RMCAnnouncementTemplate.Portrait)
            {
                portrait = GetNetEntity(speaker);
                ShowPortrait(speaker, filter, GetDisplayTime(preset, text.Length));
            }
        }

        var title = announcement.Title == null ? null : FormattedMessage.RemoveMarkupPermissive(announcement.Title);
        var msg = new RMCAnnouncementOverlayMsg(
            announcement.Preset.Id,
            text,
            title,
            announcement.Accent,
            speakerName,
            speakerJob,
            portrait);

        RaiseNetworkEvent(msg, filter);
    }

    public bool TryGetPreset(EntProtoId id, [NotNullWhen(true)] out RMCAnnouncementPresetComponent? preset)
    {
        preset = null;
        return _prototype.TryIndex(id, out var prototype) &&
               prototype.TryGetComponent(out preset, _compFactory);
    }

    public static TimeSpan GetRevealTime(RMCAnnouncementPresetComponent preset, int length)
    {
        var reveal = preset.Animation switch
        {
            TypewriterAnimationConfig typewriter => typewriter.PrintSpeed * length,
            GlitchAnimationConfig glitch => glitch.PrintSpeed * length,
            WaveAnimationConfig wave => wave.PrintSpeed * (length + wave.FadeChars),
            FadeAnimationConfig fade => fade.Duration,
            _ => TimeSpan.Zero,
        };

        return reveal > preset.MaxRevealTime ? preset.MaxRevealTime : reveal;
    }

    public static TimeSpan GetDisplayTime(RMCAnnouncementPresetComponent preset, int length)
    {
        var reveal = GetRevealTime(preset, length);
        return preset.TitleDecrypt + reveal + GetHoldTime(preset, preset.TitleDecrypt, reveal) + preset.FadeOut;
    }

    public static TimeSpan GetHoldTime(RMCAnnouncementPresetComponent preset, TimeSpan decrypt, TimeSpan reveal)
    {
        if (preset.TotalTime is not { } total)
            return preset.Hold;

        var hold = total - decrypt - reveal - preset.FadeOut;
        return hold > TimeSpan.Zero ? hold : TimeSpan.Zero;
    }

    private void ShowPortrait(EntityUid speaker, Filter filter, TimeSpan displayTime)
    {
        var until = _timing.CurTime + displayTime + _portraitGrace;
        var viewers = EnsureComp<RMCAnnouncementPortraitViewersComponent>(speaker);
        foreach (var session in filter.Recipients)
        {
            if (viewers.Viewers.TryGetValue(session, out var existing))
            {
                if (until > existing)
                    viewers.Viewers[session] = until;

                continue;
            }

            viewers.Viewers[session] = until;
            _pvsOverride.AddSessionOverride(speaker, session);
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCAnnouncementPortraitViewersComponent>();
        while (query.MoveNext(out var uid, out var viewers))
        {
            foreach (var (session, until) in viewers.Viewers)
            {
                if (time >= until)
                    _expired.Add(session);
            }

            foreach (var session in _expired)
            {
                viewers.Viewers.Remove(session);
                _pvsOverride.RemoveSessionOverride(uid, session);
            }

            _expired.Clear();
            if (viewers.Viewers.Count == 0)
                RemCompDeferred<RMCAnnouncementPortraitViewersComponent>(uid);
        }
    }
}
