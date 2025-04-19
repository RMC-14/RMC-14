using Content.Shared._RMC14.Marines;
using Content.Shared.Ghost;
using Content.Shared.Actions;
using Content.Shared._RMC14.Mobs;
using Content.Shared.Overlays;

namespace Content.Server._RMC14.Mobs
{
    public sealed class CMGhostSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actions = default!;

        public override void Initialize()
        {
            base.Initialize();

            //This shit is so scuffed but honest to god not sure what else I can use that isn't a duplicate
            SubscribeLocalEvent<GhostHearingComponent, ComponentStartup>(OnGhostStartup);
            SubscribeLocalEvent<CMGhostComponent, ComponentStartup>(OnCMGhostStartup);

            SubscribeLocalEvent<CMGhostComponent, ToggleMarineHudActionEvent>(OnMarineHudAction);
            SubscribeLocalEvent<CMGhostComponent, ToggleXenoHudActionEvent>(OnXenoHudAction);
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
            EnsureComp<ShowHealthIconsComponent>(uid);
            EnsureComp<CMGhostXenoHudComponent>(uid);
        }

        private void OnMarineHudAction(EntityUid uid, CMGhostComponent comp, ToggleMarineHudActionEvent args)
        {
            args.Handled = true;

            if (HasComp<ShowMarineIconsComponent>(uid))
            {
                RemComp<ShowMarineIconsComponent>(uid);
                RemCompDeferred<ShowHealthIconsComponent>(uid);
                _actions.SetToggled(comp.ToggleMarineHudEntity, true);
            }
            else
            {
                AddComp<ShowMarineIconsComponent>(uid);
                EnsureComp<ShowHealthIconsComponent>(uid);
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
    }
}
