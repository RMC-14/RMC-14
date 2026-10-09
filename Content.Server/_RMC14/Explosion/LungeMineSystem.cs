using Content.Server.Body.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared._RMC14.Stun;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Server._RMC14.Explosion;

public sealed class LungeMineSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly RMCDazedSystem _dazed = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LungeMineComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<LungeMineComponent, TriggerEvent>(OnTrigger);
    }

    private void OnMeleeHit(Entity<LungeMineComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        foreach (var target in args.HitEntities)
        {
            if (target == args.User)
                continue;

            _audio.PlayPvs(ent.Comp.TriggerSound, ent);
            _transform.SetCoordinates(ent, Transform(target).Coordinates);
            _trigger.Trigger(ent, args.User);
            return;
        }
    }

    private void OnTrigger(EntityUid uid, LungeMineComponent component, TriggerEvent args)
    {
        if (args.User is not { } user || TerminatingOrDeleted(user))
            return;

        _dazed.TryDaze(user, component.DazeDuration, true, stutter: true);

        if (component.GibsWielder)
            _body.GibBody(user, true);
    }
}
