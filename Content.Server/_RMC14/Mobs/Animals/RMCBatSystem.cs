using Content.Server.NPC.Systems;
using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Damage;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCBatSystem : RMCAnimalSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCBatHangingComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RMCBatHangingComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCBatHangingComponent>();
        while (query.MoveNext(out var uid, out var bat))
        {
            if (bat.NextCheckAt > now)
                continue;

            bat.NextCheckAt = now + bat.CheckCooldown;

            if (ActorQuery.HasComp(uid) || !MobState.IsAlive(uid))
            {
                WakeBat((uid, bat));
                continue;
            }

            if (bat.Hanging)
            {
                StabilizeBat(uid);

                if (Random.Prob(bat.WakeChance) || TryWakeFromDisturbance((uid, bat)))
                    WakeBat((uid, bat));
            }
            else if (Random.Prob(bat.HangChance) && CanHang(uid, bat))
            {
                HangBat((uid, bat));
            }
        }
    }

}
