using Content.Shared._CM14.Attachable;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;


namespace Content.Client._CM14.Attachable;

public sealed class AttachableHolderVisualsSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<AttachableHolderVisualsComponent, AttachableHolderAttachablesAlteredEvent>(OnAttachablesAltered);
    }
    
    private void OnAttachablesAltered(Entity<AttachableHolderVisualsComponent> holder, ref AttachableHolderAttachablesAlteredEvent args)
    {
        if(!_entityManager.TryGetComponent<AttachableVisualsComponent>(args.AttachableUid, out AttachableVisualsComponent? attachableComponent))
            return;
        
        if(args.Attached)
        {
            AddAttachableOverlay(holder.Owner, holder.Comp, attachableComponent, args.SlotID);
            return;
        }
        RemoveAttachableOverlay(holder.Owner, holder.Comp, args.SlotID);
    }
    
    private void AddAttachableOverlay(EntityUid holderUid, AttachableHolderVisualsComponent holderComponent, AttachableVisualsComponent attachableComponent, string slotID)
    {
        if(!holderComponent.Offsets.ContainsKey(slotID) || !_entityManager.TryGetComponent<SpriteComponent>(holderUid, out SpriteComponent? spriteComponent))
            return;
        
        if(attachableComponent.Rsi == null || attachableComponent.Prefix == null)
            return;
        
        PrototypeLayerData layerData = new PrototypeLayerData()
        {
            RsiPath = attachableComponent.Rsi,
            State = attachableComponent.Prefix + slotID,
            Offset = holderComponent.Offsets[slotID],
            Visible = true
        };
        
        spriteComponent.LayerMapSet(slotID, spriteComponent.AddLayer(layerData));
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
}
