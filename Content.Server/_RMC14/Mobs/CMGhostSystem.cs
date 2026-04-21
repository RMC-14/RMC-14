using Content.Server.Ghost;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Mobs;
using Content.Shared._RMC14.PropCalling;
using Content.Shared.Actions;
using Content.Shared.Ghost;
using Content.Shared.Overlays;
using Robust.Shared.Configuration;

namespace Content.Server._RMC14.Mobs
{
    public sealed class CMGhostSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly IConfigurationManager _configuration = default!;
        [Dependency] private readonly SharedMarineSystem _marine = default!;

        private bool _ghostsCanBoo;

        public override void Initialize()
        {
            base.Initialize();

            //This shit is so scuffed but honest to god not sure what else I can use that isn't a duplicate
            SubscribeLocalEvent<GhostHearingComponent, ComponentStartup>(OnGhostStartup);
            SubscribeLocalEvent<CMGhostComponent, ComponentStartup>(OnCMGhostStartup);
            SubscribeLocalEvent<CMGhostComponent, MapInitEvent>(OnCMGhostInit, after: [typeof(GhostSystem)]);

            SubscribeLocalEvent<CMGhostComponent, ToggleMarineHudActionEvent>(OnMarineHudAction);
            SubscribeLocalEvent<CMGhostComponent, ToggleXenoHudActionEvent>(OnXenoHudAction);

            Subs.CVar(_configuration, RMCCVars.RMCGhostCanBoo, OnGhostBooChange, true);
        }

        private void OnGhostStartup(EntityUid uid, GhostHearingComponent comp, ComponentStartup args)
        {
            EnsureComp<CMGhostComponent>(uid);
        }

        private void OnCMGhostStartup(EntityUid uid, CMGhostComponent comp, ComponentStartup args)
        {
            _actions.AddAction(uid, ref comp.ToggleMarineHudEntity, comp.ToggleMarineHud);
            _actions.AddAction(uid, ref comp.ToggleXenoHudEntity, comp.ToggleXenoHud);
            _actions.AddAction(uid, ref comp.FindParasiteEntity, comp.FindParasite);

            EnsureComp<ShowMarineIconsComponent>(uid);
            var bars = EnsureComp<ShowHealthBarsComponent>(uid);
            bars.DamageContainers.Add("Biological");
            EnsureComp<ShowHealthIconsComponent>(uid);
            EnsureComp<CMGhostXenoHudComponent>(uid);
            EnsureComp<PropCallingComponent>(uid);
        }

        private void OnMarineHudAction(EntityUid uid, CMGhostComponent comp, ToggleMarineHudActionEvent args)
        {
            args.Handled = true;

            if (HasComp<ShowMarineIconsComponent>(uid))
            {
                RemComp<ShowMarineIconsComponent>(uid);
                RemCompDeferred<ShowHealthIconsComponent>(uid);
                RemCompDeferred<ShowHealthBarsComponent>(uid);
                _actions.SetToggled(comp.ToggleMarineHudEntity, true);
            }
            else
            {
                EnsureComp<ShowHealthIconsComponent>(uid);

                _marine.GiveMarineHud(uid, null, true);

                var bars = EnsureComp<ShowHealthBarsComponent>(uid);
                bars.DamageContainers.Add("Biological");

                _actions.SetToggled(comp.ToggleMarineHudEntity, false);
            }
        }
        private void OnXenoHudAction(EntityUid uid, CMGhostComponent comp, ToggleXenoHudActionEvent args)
        {
            args.Handled = true;

            if (HasComp<CMGhostXenoHudComponent>(uid))
            {
                RemComp<CMGhostXenoHudComponent>(uid);
                _actions.SetToggled(comp.ToggleXenoHudEntity, true);
            }
            else
            {
                AddComp<CMGhostXenoHudComponent>(uid);
                _actions.SetToggled(comp.ToggleXenoHudEntity, false);
            }
        }

        private void OnCMGhostInit(Entity<CMGhostComponent> cmghost, ref MapInitEvent args)
        {
            if (TryComp<GhostComponent>(cmghost, out var ghost))
                ChangeGhostBoo((cmghost, ghost));
        }

        private void OnGhostBooChange(bool value, in CVarChangeInfo info)
        {
            _ghostsCanBoo = value;
            var query = EntityQueryEnumerator<GhostComponent>();
            while (query.MoveNext(out var uid, out var ghost))
            {
                ChangeGhostBoo((uid, ghost));
            }
        }

        private void ChangeGhostBoo(Entity<GhostComponent> ghost)
        {
            if (_ghostsCanBoo)
            {
                _actions.AddAction(ghost.Owner, ref ghost.Comp.BooActionEntity, ghost.Comp.BooAction);
            }
            else
            {
                _actions.RemoveAction(ghost.Owner, ghost.Comp.BooActionEntity);
            }
        }
    }
}
