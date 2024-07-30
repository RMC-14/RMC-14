using Content.Shared._RMC14.Stealth;
using Content.Shared.Foldable;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;

namespace Content.Shared._RMC14.Stealth;

public sealed class RMCPassiveStealthSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly FoldableSystem _foldable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCPassiveStealthComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RMCPassiveStealthComponent, FoldedEvent>(OnFolded, after:[typeof(SharedEntityStorageSystem)]);

    SubscribeLocalEvent<RMCPassiveStealthComponent, ActivateInWorldEvent>(OnToggle);
    }

    private void OnInit(Entity<RMCPassiveStealthComponent> ent, ref ComponentInit args)
    {
        if (Paused(ent.Owner))
            return;

        ent.Comp.Enabled = false;
        EnsureComp<EntityTurnInvisibleComponent>(ent.Owner);
    }

    private void OnFolded(Entity<RMCPassiveStealthComponent> ent, ref FoldedEvent args)
    {
        if (ent.Comp.Enabled == null)
            return;

        if (!args.IsFolded)
        {
            _entityStorage.OpenStorage(ent.Owner);
            return;
        }
        _entityStorage.OpenStorage(ent.Owner);
        ent.Comp.Enabled = false;
        RemCompDeferred<EntityActiveInvisibleComponent>(ent.Owner);
    }

    private void OnToggle(Entity<RMCPassiveStealthComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!ent.Comp.Toggleable)
            return;

        if(ent.Comp.Enabled == null)
            ent.Comp.Enabled = false;

        if (TryComp<FoldableComponent>(ent.Owner, out var fold) && fold.IsFolded)
            return;

        if (!_whitelist.IsValid(ent.Comp.Whitelist, args.User))
        {
            var popup = Loc.GetString("rmc-skills-cant-use", ("item", ent.Owner));
            _popup.PopupClient(popup, args.User, args.User, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        if (ent.Comp.Enabled.Value)
        {
            ent.Comp.Enabled = false;
            RemCompDeferred<EntityActiveInvisibleComponent>(ent.Owner);
            return;
        }

        ent.Comp.Enabled = true;
        var invisibility = EnsureComp<EntityActiveInvisibleComponent>(ent.Owner);
        invisibility.Opacity = ent.Comp.MinOpacity;
    }
}
