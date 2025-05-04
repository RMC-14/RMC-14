using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Inhands;

public sealed class XenoInhandsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoInhandsComponent, DidEquipHandEvent>(OnXenoSpritePickedUp);
        SubscribeLocalEvent<XenoInhandsComponent, DidUnequipHandEvent>(OnXenoSpriteDropped);
    }

    public void OnXenoSpritePickedUp(Entity<XenoInhandsComponent> xeno, ref DidEquipHandEvent args)
    {
        UpdateHand(args.User, args.Hand);
    }

    public void OnXenoSpriteDropped(Entity<XenoInhandsComponent> xeno, ref DidUnequipHandEvent args)
    {
        UpdateHand(args.User, args.Hand);
    }
    private void UpdateHand(EntityUid user, Hand hand)
    {
        if (!TryComp<XenoInhandsComponent>(user, out var inhands))
            return;

        if (hand.Location == HandLocation.Middle)
            return;

        string held = string.Empty;

        if (hand.HeldEntity != null)
        {
            if (TryComp<XenoInhandSpriteComponent>(hand.HeldEntity, out var inhandSprite))
            {
                held = inhandSprite.StateName ?? string.Empty;
            }
        }

        _appearance.SetData(user,
        hand.Location == HandLocation.Left ? XenoInhandVisuals.LeftHand : XenoInhandVisuals.RightHand,
        held);
    }
}
