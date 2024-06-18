using Content.Shared._CM14.Medical.Components;
using Content.Shared._CM14.Medical.Events;
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
        SubscribeLocalEvent<HolocardStateComponent, OpenChangeHolocardUIEvent>(OpenChangeHolocardUI);
    }

    private void ChangeHolocard(EntityUid entity, HolocardStateComponent comp, ref HolocardChangeEvent args)
    {

    }
    private void OpenChangeHolocardUI(EntityUid entity, HolocardStateComponent comp, ref OpenChangeHolocardUIEvent args)
    {
        var localOwner = _entityManager.GetEntity(args.Owner);
        var localTarget = _entityManager.GetEntity(args.Target);
    }
}
