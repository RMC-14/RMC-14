using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.UserInterface;

namespace Content.Client._RMC14.Botany;

public sealed class GeneEditorUISystem : EntitySystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GeneEditorComponent, AfterAutoHandleStateEvent>(OnRefresh);
    }

    private void OnRefresh(Entity<GeneEditorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _rmcUI.RefreshUIs<GeneEditorBui>(ent.Owner);
    }
}
