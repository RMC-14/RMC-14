using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Projectile.Parasite;
using Content.Shared.Verbs;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction.EggMorpher;

public sealed partial class EggMorpherSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EggMorpherComponent, XenoChangeParasiteReserveMessage>(OnChangeParasiteReserve);
        SubscribeLocalEvent<EggMorpherComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _time.CurTime;
        var eggMorpherQuery = EntityQueryEnumerator<EggMorpherComponent>();
        while (eggMorpherQuery.MoveNext(out var eggMorpherEnt, out var eggMorpherComp))
        {
            if (eggMorpherComp.GrowMaxParasites <= eggMorpherComp.CurParasites)
            {
                continue;
            }

            var newSpawnTime = GetParasiteSpawnCooldown((eggMorpherEnt, eggMorpherComp)) + curTime;

            if (eggMorpherComp.NextSpawnAt < curTime)
            {
                eggMorpherComp.CurParasites++;
                eggMorpherComp.NextSpawnAt = newSpawnTime;
                continue;
            }

            if (newSpawnTime < eggMorpherComp.NextSpawnAt || eggMorpherComp.NextSpawnAt is null)
            {
                eggMorpherComp.NextSpawnAt = newSpawnTime;
            }
        }
    }

    private void OnChangeParasiteReserve(Entity<EggMorpherComponent> eggMorpher, ref XenoChangeParasiteReserveMessage args)
    {
        eggMorpher.Comp.ReservedParasites = args.NewReserve;
    }

    private void OnGetVerbs(Entity<EggMorpherComponent> eggMorpher, ref GetVerbsEvent<ActivationVerb> args)
    {
        var user = args.User;
        if (!_hive.FromSameHive(user, eggMorpher.Owner))
        {
            return;
        }

        var changeReserveVerb = new ActivationVerb()
        {
            Text = Loc.GetString("xeno-reserve-parasites-verb"),
            Act = () =>
            {
                _ui.OpenUi(eggMorpher.Owner, XenoReserveParasiteChangeUI.Key, user);

            }
        };

        args.Verbs.Add(changeReserveVerb);
    }

    private TimeSpan GetParasiteSpawnCooldown(Entity<EggMorpherComponent> eggMorpher)
    {
        if (_hive.GetHive(eggMorpher.Owner) is not Entity<HiveComponent> hive)
        {
            return eggMorpher.Comp.StandardSpawnCooldown;
        }

        if (hive.Comp.CurrentQueen is EntityUid curQueen &&
            HasComp<XenoAttachedOvipositorComponent>(curQueen))
        {
            return eggMorpher.Comp.OviSpawnCooldown;
        }

        return eggMorpher.Comp.StandardSpawnCooldown;
    }
}
