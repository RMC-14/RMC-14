using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared._RMC14.Xenonids.Projectile.Parasite;
using Content.Shared.Hands.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._RMC14.Xenonids.Projectile.Parasite;

public sealed partial class XenoParasiteThrowerSystem : SharedXenoParasiteThrowerSystem
{
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRole = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    protected override void GetParasiteFromInventory(Entity<XenoParasiteThrowerComponent> xeno, out string? msg)
    {
        var (ent, comp) = xeno;
        var addGhostRoleProb = comp.ParasiteGhostRoleProbability;

        if (!_container.TryGetContainer(ent, XenoParasiteThrowerComponent.ParasiteContainerId, out var parasiteContainer))
        {
            msg = null;
            return;
        }
        base.GetParasiteFromInventory(xeno, out _);

        var curParasiteCount = parasiteContainer.Count;
        if (curParasiteCount == 0)
        {
            msg = Loc.GetString("cm-xeno-throw-parasite-no-parasites");
            return;
        }

        var parasite = parasiteContainer.ContainedEntities.FirstOrNull();

        if (parasite is null)
        {
            msg = null;
            return;
        }

        if (_random.NextDouble() < addGhostRoleProb)
        {
            EnsureComp<GhostRoleComponent>(parasite.Value);
        }

        _hands.TryPickupAnyHand(ent, parasite.Value);
        --curParasiteCount;

        msg = Loc.GetString("cm-xeno-throw-parasite-unstash-parasite", ("cur_parasites", curParasiteCount), ("max_parasites", comp.MaxParasites));

        if (TryComp(ent, out XenoParasiteThrowerComponent? throwerComp))
        {
            SetReservedParasites(parasiteContainer, throwerComp.ReservedParasites);
        }
    }

    protected override void SetReservedParasites(BaseContainer parasiteContainer, int reserveCount)
    {
        base.SetReservedParasites(parasiteContainer, reserveCount);

        HashSet<EntityUid> curReserved = new();
        HashSet<EntityUid> curUnreserved = new();

        foreach (var parasite in parasiteContainer.ContainedEntities)
        {
            if (!HasComp<GhostRoleComponent>(parasite))
            {
                curReserved.Add(parasite);
            }
            else
            {
                curUnreserved.Add(parasite);
            }
        }
        var difference = Math.Abs(curReserved.Count - reserveCount);
        if (curReserved.Count > reserveCount)
        {
            foreach (var parasite in curUnreserved)
            {
                if (difference == 0)
                {
                    return;
                }

                RemCompDeferred<GhostRoleComponent>(parasite);
                --difference;
            }
        }

        if (curReserved.Count < reserveCount)
        {
            foreach (var parasite in curReserved)
            {
                if (difference == 0)
                {
                    return;
                }

                AddComp<GhostRoleComponent>(parasite);
                --difference;
            }
        }

    }
}
