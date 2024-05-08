using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Sound;

public sealed class CMSoundSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RandomSoundComponent, MapInitEvent>(OnRandomMapInit);

        SubscribeLocalEvent<SoundOnDeathComponent, MobStateChangedEvent>(OnDeathMobStateChanged);
    }

    private void OnRandomMapInit(Entity<RandomSoundComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.PlayAt = _timing.CurTime + _random.Next(ent.Comp.Min, ent.Comp.Max);
        Dirty(ent);
    }

    private void OnDeathMobStateChanged(Entity<SoundOnDeathComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (_net.IsServer)
            _audio.PlayPvs(ent.Comp.Sound, ent);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var random = EntityQueryEnumerator<RandomSoundComponent>();
        while (random.MoveNext(out var uid, out var comp))
        {
            if (_mobState.IsDead(uid) || time <= comp.PlayAt)
                continue;

            comp.PlayAt = time + _random.Next(comp.Min, comp.Max);
            Dirty(uid, comp);

            _audio.PlayPvs(comp.Sound, uid);
        }
    }
}
