using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Humanoid;
using Content.Server._RMC14.Preferences;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed class AntagLoadProfileRuleSystem : GameRuleSystem<AntagLoadProfileRuleComponent>
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly RMCCharacterSelectionSystem _characters = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagLoadProfileRuleComponent, AntagSelectEntityEvent>(OnSelectEntity);
    }

    private void OnSelectEntity(Entity<AntagLoadProfileRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Handled)
            return;

        // RMC14
        HumanoidCharacterProfile? profile = null;
        if (args.Session != null)
        {
            var roles = new HashSet<ProtoId<AntagPrototype>>();
            foreach (var (definition, selectedSessions) in args.GameRule.Comp.PreSelectedSessions)
            {
                if (selectedSessions.Contains(args.Session))
                    roles.UnionWith(definition.PrefRoles);
            }

            if (roles.Count == 0)
            {
                foreach (var definition in args.GameRule.Comp.Definitions)
                {
                    roles.UnionWith(definition.PrefRoles);
                }
            }

            if (roles.Count > 0)
                profile = _characters.ResolveAntagCharacter(args.Session.UserId, roles);

            profile ??= _characters.GetDefaultProfile(args.Session.UserId);
        }

        profile ??= HumanoidCharacterProfile.RandomWithSpecies();
        // RMC14


        if (profile?.Species is not { } speciesId || !_proto.TryIndex(speciesId, out var species))
        {
            species = _proto.Index<SpeciesPrototype>(SharedHumanoidAppearanceSystem.DefaultSpecies);
        }

        if (ent.Comp.SpeciesOverride != null
            && (ent.Comp.SpeciesOverrideBlacklist?.Contains(new ProtoId<SpeciesPrototype>(species.ID)) ?? false))
        {
            species = _proto.Index(ent.Comp.SpeciesOverride.Value);
        }

        args.Entity = Spawn(species.Prototype);
        _humanoid.LoadProfile(args.Entity.Value, profile?.WithSpecies(species.ID));
    }
}
