using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Rules;

public sealed class SharedCMDistressSignalSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public bool GetPreferredJobVariant(HumanoidCharacterProfile profile, JobPrototype job, [NotNullWhen(true)] out JobPrototype? variant)
    {
        if (profile.PreferredJobVariants.TryGetValue(job.ID, out var newVariant))
        {
            if (_prototypes.TryIndex<JobPrototype>(newVariant, out var final))
            {
                variant = final;
                return true;
            }
        }

        variant = null;
        return false;
    }
}
