using Content.Shared._RMC14.Chat;
using Content.Shared.Mobs;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Deafness;

public abstract class SharedDeafnessSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly SharedCMChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public ProtoId<StatusEffectPrototype> DeafKey = "Deaf";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeafenWhileCritComponent, StatusEffectEndedEvent>(OnCanHear);
        SubscribeLocalEvent<DeafenWhileCritComponent, MobStateChangedEvent>(OnDeafenWhileCritMobState);

        SubscribeLocalEvent<ActiveDeafenWhileCritComponent, MobStateChangedEvent>(OnActiveDeafenWhileCritMobState);
    }

    private void OnCanHear(Entity<DeafenWhileCritComponent> ent, ref StatusEffectEndedEvent args)
    {
        if (args.Key != DeafKey)
            return;

        var msg = Loc.GetString("rmc-deaf-end");
        _chat.ChatMessageToOne(msg, ent.Owner);
    }

    private void OnDeafenWhileCritMobState(Entity<DeafenWhileCritComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical)
            return;

        EnsureComp<ActiveDeafenWhileCritComponent>(ent);
    }

    private void OnActiveDeafenWhileCritMobState(Entity<ActiveDeafenWhileCritComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical)
            RemCompDeferred<ActiveDeafenWhileCritComponent>(ent);
    }

    public bool TryDeafen(EntityUid uid, TimeSpan time, bool refresh = false, StatusEffectsComponent? status = null, bool force = false)
    {
        if (!Resolve(uid, ref status, false))
            return false;

        if (time <= TimeSpan.Zero)
            return false;

        if (!HasComp<DeafComponent>(uid)) // First time being deafened
        {
            var msg = Loc.GetString("rmc-deaf-start");
            _chat.ChatMessageToOne(msg, uid);
        }

        if (!_statusEffect.TryAddStatusEffect<DeafComponent>(uid, DeafKey, time, refresh, force: force))
            return false;

        var ev = new RMCDeafenedEvent(time);
        RaiseLocalEvent(uid, ref ev);

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;

        var activeQuery = EntityQueryEnumerator<ActiveDeafenWhileCritComponent, StatusEffectsComponent>();
        while (activeQuery.MoveNext(out var uid, out var comp, out var status))
        {
            if (comp.AddAt < time)
            {
                comp.AddAt = time + comp.Every;
                TryDeafen(uid, comp.Add, true, status);
            }
        }
    }
}

/// <summary>
///     Raised directed on an entity when it is made deaf.
/// </summary>
[ByRefEvent]
public record struct RMCDeafenedEvent(TimeSpan Duration);
