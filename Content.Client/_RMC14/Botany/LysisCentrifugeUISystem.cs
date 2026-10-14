using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.UserInterface;

namespace Content.Client._RMC14.Botany;

public sealed class LysisCentrifugeUISystem : EntitySystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LysisCentrifugeComponent, AfterAutoHandleStateEvent>(OnRefresh);
    }

    private void OnRefresh(Entity<LysisCentrifugeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _rmcUI.RefreshUIs<LysisCentrifugeBui>(ent.Owner);
    }
}
