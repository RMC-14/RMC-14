using Content.Server.Shuttles.Events;
using Content.Shared._RMC14.Shuttles;
using Robust.Server.Audio;

namespace Content.Server._RMC14.Shuttles;

public sealed class RMCShuttleSystem : SharedRMCShuttleSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlaySoundOnFTLStartComponent, FTLStartedEvent>(OnPlaySoundOnFTLStart);
    }

    private void OnPlaySoundOnFTLStart(Entity<PlaySoundOnFTLStartComponent> ent, ref FTLStartedEvent args)
    {
        if (Transform(ent).GridUid is not { } grid)
            return;

        _audio.PlayPvs(ent.Comp.Sound, grid);
        RemCompDeferred<PlaySoundOnFTLStartComponent>(ent);
    }
}
