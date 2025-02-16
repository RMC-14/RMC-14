using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Strain;

public sealed class XenoStrainSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoStrainComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<XenoStrainComponent, NewXenoEvolvedEvent>(OnNewXenoEvolved);
    }

    private void OnExamined(Entity<XenoStrainComponent> ent, ref ExaminedEvent args)
    {
        if (string.IsNullOrWhiteSpace(ent.Comp.Name))
            return;

        using (args.PushGroup(nameof(XenoStrainComponent)))
        {
            args.PushText(Loc.GetString("rmc-xeno-strain-specialized-into", ("strain", Loc.GetString(ent.Comp.Name))));
        }
    }

    private void OnNewXenoEvolved(Entity<XenoStrainComponent> ent, ref NewXenoEvolvedEvent args)
    {
        if (ent.Comp.Popup is not { } popup)
            return;

        if (_net.IsClient)
            return;

        _popup.PopupEntity(Loc.GetString(popup), ent, ent, PopupType.MediumXeno);
    }

    public bool AreSameStrain(Entity<XenoStrainComponent?> one, Entity<XenoStrainComponent?> two)
    {
        if (!Resolve(one, ref one.Comp, false) ||
            !Resolve(two, ref two.Comp, false))
        {
            return false;
        }

        return one.Comp.Name == two.Comp.Name;
    }
}
