using System.Globalization;
using Content.Client._RMC14.Medical.HUD;
using Content.Client.Atmos.Rotting;
using Content.Client.Message;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.Defibrillator;
using Content.Shared._RMC14.Medical.HUD;
using Content.Shared._RMC14.Medical.HUD.Components;
using Content.Shared._RMC14.Medical.HUD.Systems;
using Content.Shared._RMC14.Medical.Scanner;
using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Shared._RMC14.Medical.Wounds;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Temperature;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Medical.Scanner;

public sealed class HealthScannerUiData
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private HealthScannerWindow? _holocardWindow;
    private NetEntity _lastTarget;

    private readonly ShowHolocardIconsSystem _holocardIcons;
    private readonly MobStateSystem _mob;
    private readonly MobThresholdSystem _mobThresholds;
    private readonly RMCReagentSystem _rmcReagent;
    private readonly RottingSystem _rot;
    private readonly SkillsSystem _skills;
    private readonly RMCUnrevivableSystem _unrevivable;
    private readonly SharedWoundsSystem _wounds;

    private readonly Dictionary<EntProtoId<SkillDefinitionComponent>, int> _bloodPackSkill = new() { ["RMCSkillSurgery"] = 1 };
    private readonly Dictionary<EntProtoId<SkillDefinitionComponent>, int> _defibSkill = new() { ["RMCSkillMedical"] = 2 };
    private readonly Dictionary<EntProtoId<SkillDefinitionComponent>, int> _larvaSurgerySkill = new() { ["RMCSkillSurgery"] = 2 };

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<DamageGroupPrototype> AirlossGroup = "Airloss";
    private static readonly ProtoId<DamageGroupPrototype> GeneticGroup = "Genetic";

    public HealthScannerUiData()
    {
        IoCManager.InjectDependencies(this);

        _holocardIcons = _entities.System<ShowHolocardIconsSystem>();
        _mob = _entities.System<MobStateSystem>();
        _mobThresholds = _entities.System<MobThresholdSystem>();
        _rmcReagent = _entities.System<RMCReagentSystem>();
        _rot = _entities.System<RottingSystem>();
        _skills = _entities.System<SkillsSystem>();
        _unrevivable = _entities.System<RMCUnrevivableSystem>();
        _wounds = _entities.System<SharedWoundsSystem>();
    }

    public void PopulateHealthScan(HealthScannerWindow window, HealthScanState uiState)
    {
        if (_entities.GetEntity(uiState.Target) is not { Valid: true } target)
            return;

        _lastTarget = uiState.Target;

        window.PatientLabel.Text = Loc.GetString("rmc-health-analyzer-patient", ("name", Identity.Name(target, _entities, _player.LocalEntity)));

        if (!_entities.TryGetComponent(target, out DamageableComponent? damageable))
            return;

        var ent = new Entity<DamageableComponent>(target, damageable);
        AddGroup(ent, window.BruteLabel, Color.FromHex("#DF3E3E"), BruteGroup, Loc.GetString("rmc-health-analyzer-brute"));
        AddGroup(ent, window.BurnLabel, Color.FromHex("#FFB833"), BurnGroup, Loc.GetString("rmc-health-analyzer-burn"));
        AddGroup(ent, window.ToxinLabel, Color.FromHex("#25CA4C"), ToxinGroup, Loc.GetString("rmc-health-analyzer-toxin"));
        AddGroup(ent, window.OxygenLabel, Color.FromHex("#2E93DE"), AirlossGroup, Loc.GetString("rmc-health-analyzer-oxygen"));
        if (damageable.DamagePerGroup.GetValueOrDefault(GeneticGroup) > 0)
        {
            window.CloneBox.Visible = true;
            AddGroup(ent, window.CloneLabel, Color.FromHex("#02c9c0"), GeneticGroup, Loc.GetString("rmc-health-analyzer-clone"));
        }
        else
        {
            window.CloneBox.Visible = false;
        }

        var isPermaDead = false;

        if (_mobThresholds.TryGetIncapThreshold(target, out var threshold))
        {
            var damage = threshold.Value - damageable.TotalDamage;
            window.HealthBar.MinValue = 0;
            window.HealthBar.MaxValue = 100;

            if (_mob.IsDead(target) && (_entities.HasComponent<VictimBurstComponent>(target) ||
                _rot.IsRotten(target) || _unrevivable.IsUnrevivable(target) ||
                _entities.HasComponent<RMCDefibrillatorBlockedComponent>(target)))
            {
                isPermaDead = true;
                window.HealthBar.Value = 100;
                window.HealthBar.ModulateSelfOverride = Color.Red;
                window.HealthBarText.Text = Loc.GetString("rmc-health-analyzer-permadead");
            }
            else
            {
                window.HealthBar.ModulateSelfOverride = null;
                // Scale negative values with how close to death we are - if we have a different crit and dead state
                if (damage < 0 && _mobThresholds.TryGetDeadThreshold(target, out var deadThreshold) &&
                    deadThreshold != threshold)
                    threshold = deadThreshold - threshold;

                var healthValue = damage.Float() / threshold.Value.Float() * 100f;
                window.HealthBar.Value = healthValue;

                var healthString = MathHelper.CloseTo(healthValue, 100) ? "100%" : $"{healthValue:F2}%";
                window.HealthBarText.Text = Loc.GetString("rmc-health-analyzer-healthy", ("percent", healthString));
            }
        }

        window.ChangeHolocardButton.Text = Loc.GetString("ui-health-scanner-holocard-change");
        if (_holocardWindow != window)
        {
            _holocardWindow = window;
            window.ChangeHolocardButton.OnPressed += _ => RequestOpenHolocardUi(_lastTarget);
        }

        if (_player.LocalEntity is { } viewer &&
            _skills.HasSkill(viewer, HolocardSystem.SkillType, HolocardSystem.MinimumRequiredSkill))
        {
            window.ChangeHolocardButton.Disabled = false;
            window.ChangeHolocardButton.ToolTip = "";
        }
        else
        {
            window.ChangeHolocardButton.Disabled = true;
            window.ChangeHolocardButton.ToolTip = Loc.GetString("ui-holocard-change-insufficient-skill");
        }

        if (_entities.TryGetComponent(target, out HolocardStateComponent? holocardComponent) &&
            _holocardIcons.TryGetDescription((target, holocardComponent), out var description) &&
            _holocardIcons.TryGetHolocardColor((target, holocardComponent), out var color))
        {
            window.HolocardDescription.Text = description;
            if (window.HolocardPanel.PanelOverride is StyleBoxFlat panel)
                panel.BackgroundColor = color.Value;
        }
        else
        {
            window.HolocardDescription.Text = Loc.GetString("hc-none-description");
            window.HolocardPanel.ModulateSelfOverride = null;
            if (window.HolocardPanel.PanelOverride is StyleBoxFlat panel)
                panel.BackgroundColor = Color.Transparent;
        }

        window.ChemicalsContainer.DisposeAllChildren();

        var anyChemicals = false;
        var anyUnknown = false;
        if (uiState.Chemicals != null)
        {
            foreach (var reagent in uiState.Chemicals.Contents)
            {
                if (!_rmcReagent.TryIndex(reagent.Reagent, out var prototype))
                    continue;

                if (prototype.Unknown && uiState.DetailLevel < HealthScanDetailLevel.BodyScan)
                {
                    // TODO RMC14 these shouldn't be getting sent to the client
                    anyUnknown = true;
                    continue;
                }

                var text = $"{reagent.Quantity.Float():F1} {prototype.LocalizedName}";
                if (prototype.Overdose != null && reagent.Quantity > prototype.Overdose)
                    text = $"[bold][color=red]{FormattedMessage.EscapeText(text)} OD[/color][/bold]";

                var label = new RichTextLabel();
                label.SetMarkupPermissive(text);
                window.ChemicalsContainer.AddChild(label);
                anyChemicals = true;
            }
        }

        window.UnknownReagentsLabel.SetMarkupPermissive(Loc.GetString("rmc-health-analyzer-unknown-reagents"));
        window.UnknownChemicalsPanel.Visible = anyUnknown;
        window.ChemicalContentsLabel.Visible = anyChemicals;
        window.ChemicalContentsSeparator.Visible = anyChemicals;
        window.ChemicalsContainer.Visible = anyChemicals;

        window.BloodTypeLabel.Text = "Blood:";
        var bloodMsg = new FormattedMessage();
        bloodMsg.PushColor(Color.FromHex("#25B732"));

        var percentage = uiState.MaxBlood == 0 ? 100 : uiState.Blood.Float() / uiState.MaxBlood.Float() * 100f;
        var percentageString = MathHelper.CloseTo(percentage, 100) ? "100%" : $"{percentage:F1}%";
        bloodMsg.AddText($"{percentageString}, {uiState.Blood}cl");
        bloodMsg.Pop();
        window.BloodAmountLabel.SetMessage(bloodMsg);

        if (uiState.Bleeding)
            window.Bleeding.SetMarkup(" [bold][color=#DF3E3E]\\[Bleeding\\][/color][/bold]");
        else
            window.Bleeding.SetMessage(string.Empty);

        var temperatureMsg = new FormattedMessage();
        if (uiState.Temperature is { } temperatureKelvin)
        {
            var celsius = TemperatureHelpers.KelvinToCelsius(temperatureKelvin);
            var fahrenheit = TemperatureHelpers.KelvinToFahrenheit(temperatureKelvin);
            temperatureMsg.AddText($"{celsius:F1}ºC ({fahrenheit:F1}ºF)");
        }
        else
        {
            temperatureMsg.AddText("None");
        }

        window.BodyTemperatureLabel.SetMessage(temperatureMsg);

        var pulseMsg = new FormattedMessage();
        pulseMsg.AddText(uiState.Pulse);
        window.PulseLabel.SetMessage(pulseMsg);

        window.AdviceContainer.DisposeAllChildren();
        // Medication Advice
        if (!isPermaDead)
        {
            window.MedicalAdviceLabel.Visible = true;
            window.MedicalAdviceSeparator.Visible = true;
            MedicalAdvice(ent, uiState, window);
            if (window.AdviceContainer.ChildCount == 0)
            {
                window.MedicalAdviceLabel.Visible = false;
                window.MedicalAdviceSeparator.Visible = false;
            }
        }
        else
        {
            window.MedicalAdviceLabel.Visible = false;
            window.MedicalAdviceSeparator.Visible = false;
        }
    }

    private void RequestOpenHolocardUi(NetEntity target)
    {
        if (_player.LocalEntity is not null && _entities.GetEntity(target) is { Valid: true })
            _entities.EntityNetManager.SendSystemNetworkMessage(new OpenHolocardFromScanEvent(target));
    }

    private void AddGroup(Entity<DamageableComponent> damageable, RichTextLabel label, Color color, ProtoId<DamageGroupPrototype> group, string labelStr)
    {
        var msg = new FormattedMessage();
        msg.AddText($"{labelStr}: ");
        msg.PushColor(color);

        var damage = damageable.Comp.DamagePerGroup.GetValueOrDefault(group)
            .Int()
            .ToString(CultureInfo.InvariantCulture);
        msg.AddText(_wounds.HasUntreated(damageable.Owner, group)
            ? $"{{{damage}}}"
            : $"{damage}");

        msg.Pop();
        label.SetMessage(msg);
    }

    private void MedicalAdvice(Entity<DamageableComponent> target, HealthScanState uiState, HealthScannerWindow window)
    {
        _entities.TryGetComponent(target, out WoundedComponent? wounds);
        var hasBruteWounds = false;
        var hasBurnWounds = false;

        if (wounds != null && _wounds.HasUntreated((target, wounds), wounds.BruteWoundGroup))
            hasBruteWounds = true;

        if (wounds != null && _wounds.HasUntreated((target, wounds), wounds.BurnWoundGroup))
            hasBurnWounds = true;

        if (_player.LocalEntity is not { } viewer)
            return;

        // Defibrillation related
        if (_mob.IsDead(target))
        {
            if (_mobThresholds.TryGetDeadThreshold(target, out var deadThreshold))
            {
                if (deadThreshold + 30 < target.Comp.Damage.GetTotal() &&
                    uiState.Chemicals != null &&
                    !uiState.Chemicals.ContainsReagent("CMEpinephrine", null))
                {
                    AddAdvice(Loc.GetString("rmc-health-analyzer-advice-epinephrine"), window);
                }
                else
                {
                    var defib = string.Empty;
                    if (deadThreshold - 20 <= target.Comp.Damage.GetTotal() && wounds != null && !hasBruteWounds && !hasBurnWounds)
                        defib = Loc.GetString("rmc-health-analyzer-advice-defib-repeated");
                    else if (deadThreshold > target.Comp.Damage.GetTotal())
                        defib = Loc.GetString("rmc-health-analyzer-advice-defib");

                    if (defib != string.Empty && !_skills.HasAllSkills(viewer, _defibSkill))
                        defib = $"[color=#858585]{defib}[/color]";

                    if (defib != string.Empty)
                        AddAdvice(defib, window);
                }
            }

            AddAdvice(Loc.GetString("rmc-health-analyzer-advice-cpr"), window);
        }

        // Surgery related
        if (_entities.TryGetComponent(target, out HolocardStateComponent? holocardComponent) &&
            holocardComponent.HolocardStatus == HolocardStatus.Xeno)
        {
            var larvaSurgery = Loc.GetString("rmc-health-analyzer-advice-larva-surgery");
            if (!_skills.HasAllSkills(viewer, _larvaSurgerySkill))
                larvaSurgery = $"[color=#858585]{larvaSurgery}[/color]";
            AddAdvice(larvaSurgery, window);
        }

        // TODO RMC14 more surgery advice

        // Wound related
        if (hasBruteWounds)
            AddAdvice(Loc.GetString("rmc-health-analyzer-advice-brute-wounds"), window);

        if (hasBurnWounds)
            AddAdvice(Loc.GetString("rmc-health-analyzer-advice-burn-wounds"), window);

        // Blood related
        if (uiState.Blood < uiState.MaxBlood)
        {
            var bloodPercent = uiState.Blood / uiState.MaxBlood;
            if (bloodPercent < 0.85)
            {
                var bloodPack = Loc.GetString("rmc-health-analyzer-advice-blood-pack");
                if (!_skills.HasAllSkills(viewer, _bloodPackSkill))
                    bloodPack = $"[color=#858585]{bloodPack}[/color]";
                AddAdvice(bloodPack, window);
            }

            if (bloodPercent < 0.9 && uiState.Chemicals != null && !uiState.Chemicals.ContainsReagent("Nutriment", null))
                AddAdvice(Loc.GetString("rmc-health-analyzer-advice-food"), window);
        }

        // TODO RMC14 Pain related medical advice

        // Damage related
        var brute = target.Comp.DamagePerGroup.GetValueOrDefault(BruteGroup);
        var burn = target.Comp.DamagePerGroup.GetValueOrDefault(BurnGroup);
        var toxin = target.Comp.DamagePerGroup.GetValueOrDefault(ToxinGroup);
        var airloss = target.Comp.DamagePerGroup.GetValueOrDefault(AirlossGroup);
        var genetic = target.Comp.DamagePerGroup.GetValueOrDefault(GeneticGroup);

        if (airloss > 0 && !_mob.IsDead(target))
        {
            if (airloss > 10 && _mob.IsCritical(target))
                AddAdvice(Loc.GetString("rmc-health-analyzer-advice-cpr-crit"), window);

            if (airloss > 30 && uiState.Chemicals != null && !uiState.Chemicals.ContainsReagent("CMDexalin", null))
                AddAdvice(Loc.GetString("rmc-health-analyzer-advice-dexalin"), window);
        }

        if (brute > 30 &&
            uiState.Chemicals != null &&
            !uiState.Chemicals.ContainsReagent("CMBicaridine", null) &&
            !_mob.IsDead(target))
        {
            AddAdvice(Loc.GetString("rmc-health-analyzer-advice-bicaridine"), window);
        }

        if (burn > 30 &&
            uiState.Chemicals != null &&
            !uiState.Chemicals.ContainsReagent("CMKelotane", null) &&
            !_mob.IsDead(target))
        {
            AddAdvice(Loc.GetString("rmc-health-analyzer-advice-kelotane"), window);
        }

        if (toxin > 10 &&
            uiState.Chemicals != null &&
            !uiState.Chemicals.ContainsReagent("CMDylovene", null) &&
            !uiState.Chemicals.ContainsReagent("Inaprovaline", null) &&
            !_mob.IsDead(target))
        {
            AddAdvice(Loc.GetString("rmc-health-analyzer-advice-dylovene"), window);
        }

        // TODO RMC14 Clone damage advice
    }

    private static void AddAdvice(string text, HealthScannerWindow window)
    {
        var label = new RichTextLabel();
        label.SetMarkupPermissive(text);
        window.AdviceContainer.AddChild(label);
    }
}
