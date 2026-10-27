using Content.Server.Construction;
using Content.Shared._RMC14.Chair;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;

namespace Content.Server._RMC14.Chair;

public sealed class RMCChairStackConstructionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCChairStackConstructionBlockerComponent, InteractUsingEvent>(OnInteractUsing,
            before: new[] { typeof(ConstructionSystem) });
    }

    private void OnInteractUsing(Entity<RMCChairStackConstructionBlockerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled ||
            !TryComp(ent, out RMCChairStackComponent? stack) ||
            stack.StackedCount == 0 ||
            !_tool.HasQuality(args.Used, stack.DismantleQuality))
        {
            return;
        }

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("rmc-chair-stack-cant-dismantle"), ent, args.User,
            PopupType.SmallCaution);
    }
}
