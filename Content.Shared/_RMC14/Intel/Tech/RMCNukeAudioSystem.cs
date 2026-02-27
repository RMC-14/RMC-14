using Content.Shared.Audio;
using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Toolshed.Commands.Generic;

namespace Content.Shared._RMC14.Intel.Tech;

public sealed class RMCNukeAudioSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGlobalSoundSystem _sound = default!;

    private List<SoundPathSpecifier> _audios = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCNukeAudioEvent>(OnNukeAudio);
    }

    private void OnNukeAudio(RMCNukeAudioEvent ev)
    {
        var random = new System.Random();
        var ranInt = random.Next(2);

        _audios.Add(new SoundPathSpecifier("/Audio/_RMC14/Machines/Nuke/nuke.ogg"));
        _audios.Add(new SoundPathSpecifier("/Audio/_RMC14/Machines/Nuke/nuke2-1.ogg"));

        var song = _audio.PlayGlobal(_audios[ranInt], Filter.Broadcast(), true);
    }
}
