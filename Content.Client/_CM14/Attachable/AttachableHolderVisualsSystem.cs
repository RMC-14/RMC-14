using Content.Shared._CM14.Attachable;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;


namespace Content.Client._CM14.Attachable;

public sealed class AttachableHolderVisualsSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedAttachableHolderSystem _attachableHolderSystem = default!;
    
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<AttachableHolderVisualsComponent, AttachableHolderAttachablesAlteredEvent>(OnAttachablesAltered);
    }
    
    private void OnAttachablesAltered(Entity<AttachableHolderVisualsComponent> holder, ref AttachableHolderAttachablesAlteredEvent args)
    {
        if(!_entityManager.TryGetComponent<AttachableVisualsComponent>(args.AttachableUid, out AttachableVisualsComponent? attachableComponent))
            return;
        
        switch(args.Alteration)
        {
            case AttachableAlteredType.Attached:
                SetAttachableOverlay(holder.Owner, holder.Comp, attachableComponent, args.SlotID);
                break;
                
            case AttachableAlteredType.Detached:
                RemoveAttachableOverlay(holder.Owner, holder.Comp, args.SlotID);
                break;
                
            case AttachableAlteredType.Activated:
                if(!attachableComponent.ShowActive)
                    break;
                SetAttachableOverlay(holder.Owner, holder.Comp, attachableComponent, args.SlotID, "-active");
                break;
                
            case AttachableAlteredType.Deactivated:
                if(!attachableComponent.ShowActive)
                    break;
                SetAttachableOverlay(holder.Owner, holder.Comp, attachableComponent, args.SlotID);
                break;
        }
    }
    
    private void RemoveAttachableOverlay(EntityUid holderUid, AttachableHolderVisualsComponent holderComponent, string slotID)
    {
        if(!holderComponent.Offsets.ContainsKey(slotID) || !_entityManager.TryGetComponent<SpriteComponent>(holderUid, out SpriteComponent? spriteComponent))
            return;
        
        if(!spriteComponent.LayerMapTryGet(slotID, out int index))
            return;
        
        spriteComponent.LayerMapRemove(slotID);
        spriteComponent.RemoveLayer(index);
    }
    
    private void SetAttachableOverlay(EntityUid holderUid, AttachableHolderVisualsComponent holderComponent, AttachableVisualsComponent attachableComponent, string slotID, string suffix = "")
    {
        if(!holderComponent.Offsets.ContainsKey(slotID) || !_entityManager.TryGetComponent<SpriteComponent>(holderUid, out SpriteComponent? spriteComponent))
            return;
        
        if(attachableComponent.Rsi == null || attachableComponent.Prefix == null)
            return;
        
        PrototypeLayerData layerData = new PrototypeLayerData()
        {
            RsiPath = attachableComponent.Rsi,
            State = attachableComponent.Prefix + slotID + suffix,
            Offset = holderComponent.Offsets[slotID],
            Visible = true
        };
        
        if(spriteComponent.LayerMapTryGet(slotID, out int index))
        {
            spriteComponent.LayerSetData(index, layerData);
            return;
        }
        
        spriteComponent.LayerMapSet(slotID, spriteComponent.AddLayer(layerData));
    }
}
