using Content.Shared._RMC14.Xenonids.Evolution;

namespace Content.Client._RMC14.Xenonids.Evolution;

public sealed class XenoEvolutionUISystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoEvolutionComponent, AfterAutoHandleStateEvent>(OnXenoEvolutionAfterState);
        SubscribeLocalEvent<XenoRaffleCandidateComponent, AfterAutoHandleStateEvent>(OnRaffleCandidateAfterState);
    }

    private void OnXenoEvolutionAfterState(Entity<XenoEvolutionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshEvolutionUi(ent);
    }

    private void OnRaffleCandidateAfterState(Entity<XenoRaffleCandidateComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshEvolutionUi(ent);
    }

    private void RefreshEvolutionUi(EntityUid ent)
    {
        if (!TryComp(ent, out UserInterfaceComponent? ui))
            return;

        foreach (var bui in ui.ClientOpenInterfaces.Values)
        {
            if (bui is XenoEvolutionBui evolutionBui)
                evolutionBui.Refresh();
        }
    }
}
