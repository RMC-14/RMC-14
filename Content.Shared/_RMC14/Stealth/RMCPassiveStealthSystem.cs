using Content.Shared._RMC14.Marines.Invisibility;
using Content.Shared.Foldable;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;

namespace Content.Shared._RMC14.Stealth;

public sealed class RMCPassiveStealthSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCPassiveStealthComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RMCPassiveStealthComponent, FoldedEvent>(OnFolded);
        SubscribeLocalEvent<RMCPassiveStealthComponent, ActivateInWorldEvent>(OnToggle);
    }

    private void OnInit(Entity<RMCPassiveStealthComponent> ent, ref ComponentInit args)
    {
        if (Paused(ent.Owner))
            return;

        ent.Comp.Enabled = false;
        EnsureComp<MarineTurnInvisibleComponent>(ent.Owner);
    }

    private void OnFolded(Entity<RMCPassiveStealthComponent> ent, ref FoldedEvent args)
    {
        if (!args.IsFolded)
        {
            _entityStorage.OpenStorage(ent.Owner);
            return;
        }
        ent.Comp.Enabled = false;
        RemCompDeferred<MarineActiveInvisibleComponent>(ent.Owner);
    }

    private void OnToggle(Entity<RMCPassiveStealthComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!ent.Comp.Toggleable)
            return;

        if (TryComp<FoldableComponent>(ent.Owner, out var fold) && fold.IsFolded)
            return;

        if (!_whitelist.IsValid(ent.Comp.Whitelist, args.User))
        {
            var popup = Loc.GetString("rmc-skills-cant-use", ("item", ent.Owner));
            _popup.PopupClient(popup, args.User, args.User, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        if (ent.Comp.Enabled)
        {
            ent.Comp.Enabled = false;
            RemCompDeferred<MarineActiveInvisibleComponent>(ent.Owner);
            return;
        }

        ent.Comp.Enabled = true;
        var invisibility = EnsureComp<MarineActiveInvisibleComponent>(ent.Owner);
        invisibility.Opacity = ent.Comp.MinOpacity;
    }
}
