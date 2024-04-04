using Content.Shared._CM14.Medical.Surgery;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Standing;

namespace Content.Client._CM14.Medical.Surgery;

public sealed class CMSurgerySystem : SharedCMSurgerySystem
{
    public event Action? OnRefresh;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandsComponent, DidEquipHandEvent>(RefreshUI);
        SubscribeLocalEvent<HandsComponent, DidUnequipHandEvent>(RefreshUI);
        SubscribeLocalEvent<CMSurgeryTargetComponent, DownedEvent>(RefreshUI);
        SubscribeLocalEvent<CMSurgeryTargetComponent, StoodEvent>(RefreshUI);
    }

    private void RefreshUI<TComp, TEvent>(Entity<TComp> hands, ref TEvent args) where TComp : IComponent?
    {
        OnRefresh?.Invoke();
    }
}
