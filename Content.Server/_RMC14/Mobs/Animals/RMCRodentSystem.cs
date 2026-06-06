using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCRodentSystem : RMCAnimalSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCRodentBehaviorComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RMCRodentBehaviorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCRodentBehaviorComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RMCRodentBehaviorComponent, StartCollideEvent>(OnStartCollide);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCRodentBehaviorComponent>();
        while (query.MoveNext(out var uid, out var rodent))
        {
            if (!MobState.IsAlive(uid))
                continue;

            if (ActorQuery.HasComp(uid))
            {
                WakeRodent((uid, rodent));
                continue;
            }

            if (rodent.Sleeping)
            {
                UpdateSleepingRodent((uid, rodent), now);
                continue;
            }

            if (rodent.NextThinkAt > now)
                continue;

            rodent.NextThinkAt = now + rodent.ThinkCooldown;
            if (!Container.IsEntityInContainer(uid) && Random.Prob(rodent.SleepChance))
            {
                SleepRodent((uid, rodent));
                continue;
            }

            TryRodentAmbientSqueak((uid, rodent), now);
        }
    }

}
