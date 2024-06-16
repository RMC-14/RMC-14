using Content.Shared._CM14.Attachable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;


namespace Content.Server._CM14.Attachable;

public sealed class AttachableHolderSystem : SharedAttachableHolderSystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    
    
    protected override void OnAttachDoAfter(EntityUid uid, AttachableHolderComponent component, AttachableAttachDoAfterEvent args)
    {
        base.OnAttachDoAfter(uid, component, args);
        
        if(args.Cancelled || args.Args.Target == null || args.Args.Used == null)
            return;
        
        _audioSystem.PlayPvs(EntityManager.GetComponent<AttachableComponent>(args.Args.Used.Value).AttachSound, uid);
    }
    
    protected override void OnDetachDoAfter(EntityUid uid, AttachableHolderComponent component, AttachableDetachDoAfterEvent args)
    {
        base.OnDetachDoAfter(uid, component, args);
        
        if(args.Cancelled || args.Args.Target == null || args.Args.Used == null)
            return;
        
        _audioSystem.PlayPvs(EntityManager.GetComponent<AttachableComponent>(args.Args.Used.Value).DetachSound, uid);
    }
}
