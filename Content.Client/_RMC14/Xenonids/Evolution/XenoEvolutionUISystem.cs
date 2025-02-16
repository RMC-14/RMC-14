using Content.Shared._RMC14.Xenonids.Evolution;

namespace Content.Client._RMC14.Xenonids.Evolution;

public sealed class XenoEvolutionUISystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoEvolutionComponent, AfterAutoHandleStateEvent>(OnXenoEvolutionAfterState);
    }

    private void OnXenoEvolutionAfterState(Entity<XenoEvolutionComponent> ent, ref AfterAutoHandleStateEvent args)
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
