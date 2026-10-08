using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Destrain;

public sealed class XenoDestrainSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly XenoEvolutionSystem _xenoEvolution = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoDestrainComponent, NewXenoEvolvedEvent>(OnNewXenoEvolved);

        Subs.BuiEvents<XenoDestrainComponent>(XenoDevolveUIKey.Key,
            subs =>
            {
                subs.Event<XenoDestrainBuiMsg>(OnDestrainBui);
            });
    }

    private void OnNewXenoEvolved(Entity<XenoDestrainComponent> ent, ref NewXenoEvolvedEvent args)
    {
        // Picking a strain keeps the cooldown from the base caste, evolving or devolving starts fresh
        ent.Comp.LastDestrainAt = args.OldXeno.Comp.LastDestrainAt;
        Dirty(ent);
    }

    private void OnDestrainBui(Entity<XenoDestrainComponent> ent, ref XenoDestrainBuiMsg args)
    {
        _ui.CloseUi(ent.Owner, XenoDevolveUIKey.Key, args.Actor);

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        if (ent.Comp.LastDestrainAt is { } last &&
            time < last + ent.Comp.Cooldown)
        {
            var minutes = (int)Math.Ceiling((last + ent.Comp.Cooldown - time).TotalMinutes);
            _popup.PopupEntity(Loc.GetString("rmc-xeno-destrain-cooldown", ("minutes", minutes)), ent, ent, PopupType.MediumCaution);
            return;
        }

        if (!_xenoEvolution.ContainedCheckPopup(ent))
            return;

        if (!_xenoEvolution.DamagedCheckPopup(ent, false))
            return;

        var newXeno = _xenoEvolution.DevolveInto(ent, ent.Comp.DestrainTo);
        if (TryComp(newXeno, out XenoEvolutionComponent? evolution))
        {
            evolution.LastDestrainAt = time;
            Dirty(newXeno, evolution);
        }

        _popup.PopupEntity(Loc.GetString("rmc-xeno-destrain-success"), newXeno, newXeno, PopupType.Medium);
    }
}
