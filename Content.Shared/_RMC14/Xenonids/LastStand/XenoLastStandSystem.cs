using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.LastStand;

public sealed partial class XenoLastStandSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoLastStandableComponent, NewXenoEvolvedEvent>(OnXenoEvolved);
        SubscribeLocalEvent<XenoLastStandableComponent, XenoDevolvedEvent>(OnXenoDevolved);

        SubscribeLocalEvent<XenoLastStandComponent, ComponentStartup>(OnXenoLastStandAdded);
        SubscribeLocalEvent<XenoLastStandComponent, XenoHealAttemptEvent>(OnXenoHealAttempt);
    }

    private void OnXenoHealAttempt(Entity<XenoLastStandComponent> xeno, ref XenoHealAttemptEvent args)
    {
        if (!HasComp<XenoEvolutionGranterComponent>(xeno))
            args.Cancelled = true;
    }

    private void OnXenoEvolved(Entity<XenoLastStandableComponent> xeno, ref NewXenoEvolvedEvent args)
    {
        if (TryComp<XenoLastStandComponent>(args.OldXeno, out var comp))
            CopyComp(xeno, args.NewXeno, comp);
    }

    private void OnXenoDevolved(Entity<XenoLastStandableComponent> xeno, ref XenoDevolvedEvent args)
    {
        if (TryComp<XenoLastStandComponent>(args.OldXeno, out var comp))
            CopyComp(args.OldXeno, args.NewXeno, comp);
    }

    private void OnXenoLastStandAdded(Entity<XenoLastStandComponent> xeno, ref ComponentStartup args)
    {
        if (!xeno.Comp.CallToArmsDone && _net.IsServer)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-last-stand"), xeno, xeno, PopupType.MediumXeno);
            xeno.Comp.CallToArmsDone = true;
            Dirty(xeno);
        }

        RaiseLocalEvent(xeno, new RejuvenateEvent());
    }
}
