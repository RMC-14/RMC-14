using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCCatHuntingSystem : RMCAnimalSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCCatHunterComponent, MapInitEvent>(OnCatMapInit);
        SubscribeLocalEvent<RMCCatHunterComponent, ComponentShutdown>(OnCatShutdown);
    }

    private void OnCatMapInit(Entity<RMCCatHunterComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextThinkAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.ThinkCooldown);
        ent.Comp.NextMeowAt = Timing.CurTime + RandomTime(ent.Comp.MeowCooldownMin, ent.Comp.MeowCooldownMax);
        ent.Comp.NextAmbientEmoteAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.AmbientEmoteCooldown);
    }

    private void OnCatShutdown(Entity<RMCCatHunterComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.MovementTarget = null;
        ent.Comp.PlayCounter = 0;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCCatHunterComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var hunter, out var xform))
        {
            if (!MobState.IsAlive(uid))
            {
                hunter.MovementTarget = null;
                hunter.PlayCounter = 0;
                continue;
            }

            TryMeow((uid, hunter), now);
            TryAmbientCatEmote((uid, hunter), now);

            if (ActorQuery.HasComp(uid) || hunter.NextThinkAt > now)
                continue;

            hunter.NextThinkAt = now + hunter.ThinkCooldown;

            var prey = PickPrey((uid, hunter, xform));
            if (prey == null)
            {
                hunter.MovementTarget = null;
                hunter.PlayCounter = 0;
                continue;
            }

            var preyCoords = Transform.GetMoverCoordinates(prey.Value);
            if (!Transform.GetMoverCoordinates(uid).TryDistance(EntityManager, preyCoords, out var distance))
                continue;

            if (hunter.MovementTarget != prey.Value)
            {
                hunter.MovementTarget = prey;
                hunter.PlayCounter = 0;
                Popup.PopupEntity(Loc.GetString("rmc-cat-pounces-at", ("cat", uid), ("prey", prey.Value)), uid);
            }

            TryThreatenPrey(uid, prey.Value, hunter, distance, now);

            if (distance > hunter.AttackRange)
            {
                TryMoveTowards(uid, preyCoords, hunter.MoveSpeed);
                continue;
            }

            if (hunter.PlayCounter >= hunter.MaxPlayAttacks)
            {
                hunter.MovementTarget = null;
                hunter.PlayCounter = 0;
                hunter.NextThinkAt = now + hunter.PlayBreakCooldown;
                continue;
            }

            AttackPrey(uid, prey.Value, hunter);
        }
    }

}
