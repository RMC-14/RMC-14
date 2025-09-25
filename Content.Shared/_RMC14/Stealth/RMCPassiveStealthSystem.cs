using Content.Shared.Foldable;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Stealth;

public sealed class RMCPassiveStealthSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCPassiveStealthComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RMCPassiveStealthComponent, StorageAfterOpenEvent>(OnStorageAfterOpen);
        SubscribeLocalEvent<RMCPassiveStealthComponent, FoldedEvent>(OnFolded, after:[typeof(SharedEntityStorageSystem)]);
        SubscribeLocalEvent<RMCPassiveStealthComponent, ActivateInWorldEvent>(OnToggle);
    }

    private void OnInit(Entity<RMCPassiveStealthComponent> ent, ref ComponentInit args)
    {
        if (_timing.ApplyingState)
            return;

        if (Paused(ent.Owner))
            return;

        ent.Comp.Enabled = false;
        EnsureComp<EntityTurnInvisibleComponent>(ent.Owner);
    }

    private void OnStorageAfterOpen(Entity<RMCPassiveStealthComponent> ent, ref StorageAfterOpenEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Enabled == null)
            return;

        ent.Comp.Enabled = false;
        ent.Comp.ToggleTime = _timing.CurTime;
        Dirty(ent.Owner, ent.Comp);
    }
    private void OnFolded(Entity<RMCPassiveStealthComponent> ent, ref FoldedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Enabled == null)
            return;

        if (!args.IsFolded)
        {
            _entityStorage.OpenStorage(ent.Owner);
            ent.Comp.Enabled = false;
            return;
        }
        ent.Comp.Enabled = false;
        RemCompDeferred<EntityActiveInvisibleComponent>(ent.Owner);
    }

    private void OnToggle(Entity<RMCPassiveStealthComponent> ent, ref ActivateInWorldEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (!ent.Comp.Toggleable)
            return;

        if (TryComp<FoldableComponent>(ent.Owner, out var fold) && fold.IsFolded)
            return;

        ent.Comp.Enabled ??= false;

        if (!ent.Comp.Enabled.Value && !_whitelist.IsValid(ent.Comp.Whitelist, args.User))
        {
            var popup = Loc.GetString("rmc-skills-cant-use", ("item", ent.Owner));
            _popup.PopupClient(popup, args.User, args.User, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        if (ent.Comp.Enabled.Value)
        {
            ent.Comp.Enabled = false;
            ent.Comp.ToggleTime = _timing.CurTime;
            Dirty(ent.Owner, ent.Comp);
        }
        else
        {
            ent.Comp.Enabled = true;
            ent.Comp.ToggleTime = _timing.CurTime;
            Dirty(ent.Owner, ent.Comp);
        }
    }

    public override void Update(float frameTime)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var stealth = EntityQueryEnumerator<RMCPassiveStealthComponent>();
        while (stealth.MoveNext(out var uid, out var stealthComp))
        {
            if (!stealthComp.Enabled.HasValue)
                continue;

            if(_net.IsClient)
                continue;

            var time = _timing.CurTime - stealthComp.ToggleTime;
            if (stealthComp.Enabled.Value)
            {
                var invis = EnsureComp<EntityActiveInvisibleComponent>(uid);
                if (time < stealthComp.Delay)
                {
                    invis.Opacity = (float) (stealthComp.MaxOpacity - (time / stealthComp.Delay) * (stealthComp.MaxOpacity - stealthComp.MinOpacity)); // Linear function from 1 to MinOpacity
                }
                else
                {
                    invis.Opacity = stealthComp.MinOpacity;
                }

                Dirty(uid, invis);

            }
            else
            {
                if (!TryComp<EntityActiveInvisibleComponent>(uid, out var invis))
                    continue;

                if (time < stealthComp.UnCloakDelay)
                {
                    invis.Opacity = (float) (stealthComp.MinOpacity + (time / stealthComp.UnCloakDelay) * (stealthComp.MaxOpacity - stealthComp.MinOpacity) ); // Linear function from MinOpacity to 1
                    Dirty(uid, invis);
                    continue;
                }

                RemCompDeferred<EntityActiveInvisibleComponent>(uid);
            }
        }
    }
}
