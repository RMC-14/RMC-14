using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Medical;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.Defibrillator;

public sealed class CMDefibrillatorSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DamageableComponent, TargetDefibrillatedEvent>(OnTargetDefibrillated);
        SubscribeLocalEvent<RMCDefibrillatorAudioComponent, EntityTerminatingEvent>(OnDefibrillatorAudioTerminating);
        SubscribeLocalEvent<CMDefibrillatorBlockedComponent, ExaminedEvent>(OnNoDefibExamine);
    }

    private void OnNoDefibExamine(Entity<CMDefibrillatorBlockedComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.ShowOnExamine)
            return;

        args.PushMarkup(Loc.GetString(ent.Comp.Examine, ("victim", ent)));
    }


    private void OnTargetDefibrillated(Entity<DamageableComponent> ent, ref TargetDefibrillatedEvent args)
    {
        RemComp<CMRecentlyDefibrillatedComponent>(ent);
        var comp = EnsureComp<CMRecentlyDefibrillatedComponent>(ent);
        comp.RemoveAt = _timing.CurTime + comp.RemoveAfter;
        Dirty(ent, comp);
    }

    private void OnDefibrillatorAudioTerminating(Entity<RMCDefibrillatorAudioComponent> ent, ref EntityTerminatingEvent args)
    {
        if (TryComp(ent.Comp.Defibrillator, out DefibrillatorComponent? defibrillator))
            defibrillator.ChargeSoundEntity = null;
    }

    public void StopChargingAudio(Entity<DefibrillatorComponent> defib)
    {
        _audio.Stop(defib.Comp.ChargeSoundEntity);
        QueueDel(defib.Comp.ChargeSoundEntity);
        defib.Comp.ChargeSoundEntity = null;
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<CMRecentlyDefibrillatedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (time >= comp.RemoveAt)
                RemCompDeferred<CMRecentlyDefibrillatedComponent>(uid);
        }
    }
}
