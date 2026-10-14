using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.UserInterface;

namespace Content.Client._RMC14.Botany;

public sealed class RMCPlantAnalyzerUISystem : EntitySystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCPlantAnalyzerComponent, AfterAutoHandleStateEvent>(OnRefresh);
    }

    private void OnRefresh(Entity<RMCPlantAnalyzerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _rmcUI.RefreshUIs<RMCPlantAnalyzerBui>(ent.Owner);
    }
}
