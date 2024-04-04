using Content.Shared._CM14.Medical.Surgery;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;

namespace Content.Client._CM14.Medical.Surgery;

public sealed class CMSurgerySystem : SharedCMSurgerySystem
{
    public event Action? OnRefresh;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandsComponent, DidEquipHandEvent>(RefreshUI);
        SubscribeLocalEvent<HandsComponent, DidUnequipHandEvent>(RefreshUI);
    }

    private void RefreshUI<T>(Entity<HandsComponent> hands, ref T args)
    {
        OnRefresh?.Invoke();
    }
}
