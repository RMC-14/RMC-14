using Content.Shared.Examine;

namespace Content.Shared._RMC14.Xenonids.Egg.EggRetriever;

public abstract partial class SharedXenoEggRetrieverSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoEggRetrieverComponent, ExaminedEvent>(OnEggRetrieverExamine);
    }

    private void OnEggRetrieverExamine(Entity<XenoEggRetrieverComponent> retriever, ref ExaminedEvent args)
    {
        if (!HasComp<XenoComponent>(args.Examiner))
            return;

        using (args.PushGroup(nameof(XenoEggRetrieverComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-xeno-retrieve-egg-current", ("xeno", retriever),
                ("cur_eggs", retriever.Comp.CurEggs), ("max_eggs", retriever.Comp.MaxEggs)));
        }
    }
}
