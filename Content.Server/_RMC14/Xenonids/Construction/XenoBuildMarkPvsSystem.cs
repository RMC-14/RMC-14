using Content.Shared._RMC14.Xenonids.Construction.BuildMark;
using Content.Shared.Eye;
using Robust.Server.GameStates;

namespace Content.Server._RMC14.Xenonids.Construction;

public sealed class XenoBuildMarkPvsSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoBuildMarkerComponent, ExpandPvsEvent>(OnExpandPvs);
    }

    private void OnExpandPvs(Entity<XenoBuildMarkerComponent> ent, ref ExpandPvsEvent args)
    {
        if (ent.Comp.Marks.Count == 0)
            return;

        args.Entities ??= new List<EntityUid>();
        args.Entities.AddRange(ent.Comp.Marks);
        args.VisMask |= (int) VisibilityFlags.XenoBuildMark;
    }
}
