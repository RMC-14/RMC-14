using Content.Server._RMC14.Language.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Traits;

public sealed class RMCTraitSystem : EntitySystem
{
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (args.JobId == null ||
            !_prototypeManager.TryIndex<JobPrototype>(args.JobId, out var job) ||
            !job.ApplyTraits)
        {
            return;
        }

        foreach (var traitId in args.Profile.TraitPreferences)
        {
            if (!_prototypeManager.TryIndex<TraitPrototype>(traitId, out var trait) ||
                trait.Language == null)
            {
                continue;
            }

            _language.AddLanguage(args.Mob, trait.Language.Value);
        }
    }
}
