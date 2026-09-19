using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCCorgiSystem
{
    private void OnLisaMapInit(Entity<RMCLisaCorgiComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextDanceAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.DanceCooldown);
    }

    private void UpdateLisaCorgis(TimeSpan now)
    {
        var query = EntityQueryEnumerator<RMCLisaCorgiComponent>();
        while (query.MoveNext(out var uid, out var lisa))
        {
            if (!MobState.IsAlive(uid) ||
                ActorQuery.HasComp(uid) ||
                Container.IsEntityInContainer(uid))
            {
                continue;
            }

            if (lisa.NextDanceAt <= now)
            {
                lisa.NextDanceAt = now + lisa.DanceCooldown;
                if (Random.Prob(lisa.DanceChance))
                    Popup.PopupEntity(Loc.GetString("rmc-corgi-chases-tail", ("corgi", uid)), uid);
            }
        }
    }
}
