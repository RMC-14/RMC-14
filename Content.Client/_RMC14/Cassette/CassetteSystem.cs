using Content.Shared._RMC14.Cassette;
using Content.Shared._RMC14.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Cassette;

public sealed class CassetteSystem : SharedCassetteSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IAudioManager _audioManager = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IFileDialogManager _dialogs = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private float _gain;
    private readonly Dictionary<AudioStream, string> _names = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        Subs.CVar(_config, RMCCVars.VolumeGainCassettes, SetGain, true);

        try
        {
            foreach (var entity in _prototype.EnumeratePrototypes<EntityPrototype>())
            {
                if (!entity.TryGetComponent(out CassetteTapeComponent? tape, _compFactory))
                    continue;

                foreach (var sound in tape.Songs)
                {
                    var path = _audio.GetAudioPath(_audio.ResolveSound(sound));
                    _resourceCache.TryGetResource(new ResPath(path), out AudioResource? _);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error preloading cassette songs:\n{e}");
        }
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _names.Clear();
    }

    private void SetGain(float gain)
    {
        _gain = gain;
        if (_player.LocalEntity is not { } ent)
            return;

        var slots = _inventory.GetSlotEnumerator(ent);
        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is not { } contained ||
                !TryComp(contained, out CassettePlayerComponent? player))
            {
                continue;
            }

            SetAudioGain(player.AudioStream);
            SetAudioGain(player.CustomAudioStream);
        }
    }

    protected override EntityUid? PlayCustomTrack(Entity<CassettePlayerComponent> player, Entity<CassetteTapeComponent> tape)
    {
        base.PlayCustomTrack(player, tape);
        if (tape.Comp.CustomTrack is not AudioStream stream)
            return null;

        if (!_timing.IsFirstTimePredicted)
            return null;

        if (!_names.TryGetValue(stream, out var name))
            return null;

        var audioParams = player.Comp.AudioParams.WithVolume(SharedAudioSystem.GainToVolume(_gain));
        return _audio.PlayGlobal(stream, new ResolvedPathSpecifier(name), audioParams)?.Entity;
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

            var name = $"/Audio/_RMC14/_CustomCassetteUploads/upload_{_names.Count}.ogg";
            _resourceCache.CacheResource(name, new AudioResource(audio));
            _names[audio] = name;
        }
        catch (Exception e)
        {
            Log.Error($"Error choosing custom cassette track:\n{e}");
        }
    }

    private void SetAudioGain(EntityUid? audio)
    {
        if (!TryComp(audio, out AudioComponent? audioComp))
            return;

#pragma warning disable RA0002
        audioComp.Params = audioComp.Params with { Volume = SharedAudioSystem.GainToVolume(_gain) };
#pragma warning restore RA0002
    }
}
