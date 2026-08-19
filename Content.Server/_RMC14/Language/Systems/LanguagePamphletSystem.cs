using Content.Shared._RMC14.Language.Components;
using Content.Shared._RMC14.Marines.Skills.Pamphlets;

namespace Content.Server._RMC14.Language.Systems;

public sealed class LanguagePamphletSystem : EntitySystem
{
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly LanguageLearningSystem _learning = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SkillPamphletComponent, SkillPamphletGrantLanguageEvent>(OnGrantLanguage);
    }

    private void OnGrantLanguage(Entity<SkillPamphletComponent> ent, ref SkillPamphletGrantLanguageEvent args)
    {
        _language.AddLanguage(args.User, args.Language);

        if (!TryComp<LanguageLearningComponent>(args.User, out var learning) ||
            !learning.Languages.ContainsKey(args.Language))
        {
            return;
        }

        _learning.RemoveLearnableLanguage(args.User, args.Language);
    }
}
