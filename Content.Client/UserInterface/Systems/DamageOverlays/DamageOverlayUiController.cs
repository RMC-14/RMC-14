using Content.Shared._RMC14.Medical.Pain;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Traits.Assorted;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.DamageOverlays;

[UsedImplicitly]
public sealed class DamageOverlayUiController : UIController
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    [UISystemDependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    private Overlays.DamageOverlay _overlay = default!;

    public override void Initialize()
    {
        _overlay = new Overlays.DamageOverlay();
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MobThresholdChecked>(OnThresholdCheck);
        SubscribeLocalEvent<PainLevelChangedEvent>(OnPainLevelChanged); // RMC14
    }

    private void OnPlayerAttach(LocalPlayerAttachedEvent args)
    {
        ClearOverlay();
        if (!EntityManager.TryGetComponent<MobStateComponent>(args.Entity, out var mobState))
            return;
        if (mobState.CurrentState != MobState.Dead)
            UpdateOverlays(args.Entity, mobState);
        _overlayManager.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
        ClearOverlay();
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.Target != _playerManager.LocalEntity)
            return;

        UpdateOverlays(args.Target, args.Component);
    }

    private void OnThresholdCheck(ref MobThresholdChecked args)
    {

        if (args.Target != _playerManager.LocalEntity)
            return;
        UpdateOverlays(args.Target, args.MobState, args.Damageable, args.Threshold);
    }

    // RMC14
    private void OnPainLevelChanged(ref PainLevelChangedEvent args)
    {
        if (args.Ent != _playerManager.LocalEntity)
            return;
        UpdateOverlays(args.Ent, null);
    }
    // RMC14

    private void ClearOverlay()
    {
        _overlay.DeadLevel = 0f;
        _overlay.CritLevel = 0f;
        _overlay.PainLevel = 0f;
        _overlay.OxygenLevel = 0f;
    }

    //TODO: Jezi: adjust oxygen and hp overlays to use appropriate systems once bodysim is implemented
    private void UpdateOverlays(EntityUid entity, MobStateComponent? mobState, DamageableComponent? damageable = null, MobThresholdsComponent? thresholds = null)
    {
        if (mobState == null && !EntityManager.TryGetComponent(entity, out mobState) ||
            thresholds == null && !EntityManager.TryGetComponent(entity, out thresholds) ||
            damageable == null && !EntityManager.TryGetComponent(entity, out  damageable))
            return;

        if (!_mobThresholdSystem.TryGetIncapThreshold(entity, out var foundThreshold, thresholds))
            return; //this entity cannot die or crit!!

        if (!thresholds.ShowOverlays)
        {
            ClearOverlay();
            return; //this entity intentionally has no overlays
        }

        var critThreshold = foundThreshold.Value;
        _overlay.State = mobState.CurrentState;

        switch (mobState.CurrentState)
        {
            case MobState.Alive:
            {
                FixedPoint2 painLevel = 0;
                _overlay.PainLevel = 0;

                // RMC14 Start
                if (EntityManager.TryGetComponent<PainComponent>(entity, out var painComp))
                {
                    var visiblePainPercentage = painComp.PerceivedPain;

                    // Clamp the value between the currently active level's threshold and the next level's threshold, so that the pain overlay
                    // updates per-level rather that independently of it by just using `PerceivedPain`.
                    // (It's still allowed to change to any value within the min and max, just not go past them until the current pain level changes)
                    if (painComp.PainLevels.TryGetValue(painComp.CurrentPainLevelIdx, out var lowerThreshold) &&
                        painComp.PainLevels.TryGetValue(painComp.CurrentPainLevelIdx + 1, out var upperThreshold))
                    {
                        visiblePainPercentage = FixedPoint2.Clamp(visiblePainPercentage, lowerThreshold.Threshold, upperThreshold.Threshold);
                    }

                    var newOverlayPainLevel = (visiblePainPercentage / 100).Float();
                    _overlay.PainLevel = newOverlayPainLevel < 0.05f ? 0 : newOverlayPainLevel; // Don't show damage overlay if they're near enough to max.
                    }
                else if (!EntityManager.HasComponent<PainNumbnessComponent>(entity)) // RMC14 End
                {
                    foreach (var painDamageType in damageable.PainDamageGroups)
                    {
                        damageable.DamagePerGroup.TryGetValue(painDamageType, out var painDamage);
                        painLevel += painDamage;
                    }
                    _overlay.PainLevel = FixedPoint2.Min(1f, painLevel / critThreshold).Float();

                    if (_overlay.PainLevel < 0.05f) // Don't show damage overlay if they're near enough to max.
                    {
                        _overlay.PainLevel = 0;
                    }
                }

                if (damageable.DamagePerGroup.TryGetValue("Airloss", out var oxyDamage))
                {
                    _overlay.OxygenLevel = FixedPoint2.Min(1f, oxyDamage / critThreshold).Float();
                }

                _overlay.CritLevel = 0;
                _overlay.DeadLevel = 0;
                break;
            }
            case MobState.Critical:
            {
                if (!_mobThresholdSystem.TryGetDeadPercentage(entity,
                        FixedPoint2.Max(0.0, damageable.TotalDamage), out var critLevel))
                    return;
                _overlay.CritLevel = critLevel.Value.Float();

                _overlay.PainLevel = 0;
                _overlay.DeadLevel = 0;
                break;
            }
            case MobState.Dead:
            {
                _overlay.PainLevel = 0;
                _overlay.CritLevel = 0;
                break;
            }
        }
    }
}
