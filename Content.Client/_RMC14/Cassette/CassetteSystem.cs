using Content.Shared._RMC14.Cassette;
using Robust.Client.Audio;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Cassette;

public sealed class CassetteSystem : SharedCassetteSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IAudioManager _audioManager = default!;
    [Dependency] private readonly IFileDialogManager _dialogs = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    protected override EntityUid? PlayCustomTrack(Entity<CassettePlayerComponent> player, Entity<CassetteTapeComponent> tape)
    {
        base.PlayCustomTrack(player, tape);
        if (tape.Comp.CustomTrack is not AudioStream stream)
            return null;

        return _timing.IsFirstTimePredicted
            ? _audio.PlayGlobal(stream, null, player.Comp.AudioParams)?.Entity
            : null;
    }

    protected override async void ChooseCustomTrack(Entity<CassetteTapeComponent> tape)
    {
        try
        {
            if (!_timing.IsFirstTimePredicted)
                return;

            var filters = new FileDialogFilters(new FileDialogFilters.Group("ogg"));
            await using var file = await _dialogs.OpenFile(filters);
            if (file == null)
                return;

            var audio = _audioManager.LoadAudioOggVorbis(file);
            tape.Comp.CustomTrack = audio;
        }
        catch (Exception e)
        {
            Log.Error($"Error choosing custom cassette track:\n{e}");
        }
    }
}
