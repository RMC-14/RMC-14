using Content.Shared._CM14.Medical.HUD.Components;
using Content.Shared._CM14.Medical.HUD.Events;
using Content.Shared._CM14.Medical.Scanner;
using Content.Shared._CM14.Medical.Systems;

namespace Content.Server._CM14.Medical.HUD.Systems;

public sealed class HolocardSystem : SharedHolocardSystem
{
    [Dependency] readonly private SharedUserInterfaceSystem _ui = default!;
    [Dependency] readonly private IEntityManager _entityManager = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HolocardStateComponent, HolocardChangeEvent>(ChangeHolocard);
        SubscribeLocalEvent<HealthScannerComponent, OpenChangeHolocardUIEvent>(OpenChangeHolocardUI);
    }

    private void ChangeHolocard(EntityUid entity, HolocardStateComponent comp, ref HolocardChangeEvent args)
    {

    }
    private void OpenChangeHolocardUI(EntityUid entity, HealthScannerComponent comp, ref OpenChangeHolocardUIEvent args)
    {
        OpenChangeHolocardUI(ref args);
    }
    private void OpenChangeHolocardUI(ref OpenChangeHolocardUIEvent args)
    {
        var localOwner = _entityManager.GetEntity(args.Owner);
        var localTarget = _entityManager.GetEntity(args.Target);
        _ui.OpenUi(localTarget, HolocardChangeUIKey.Key, localOwner);
    }
}
