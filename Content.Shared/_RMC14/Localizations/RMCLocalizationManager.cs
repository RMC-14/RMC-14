using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Content.Shared._RMC14.IdentityManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;

namespace Content.Shared._RMC14.Localizations;

public sealed class RMCLocalizationManager
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    public void Initialize(CultureInfo culture)
    {
        // This fixes identity popups breaking with grammar functions
        // Apparently no one has ever thought of doing this before which is awesome
        _loc.AddFunction(culture, "GENDER", FuncGender);
        _loc.AddFunction(culture, "REFLEXIVE", FuncReflexive);
        _loc.AddFunction(culture, "PROPER", FuncProper);
    }

    private ILocValue FuncGender(LocArgs args)
    {
        if (args.Args.Count < 1)
            return new LocValueString(nameof(Gender.Neuter));

        var entity0 = args.Args[0].Value;
        if (entity0 is IdentityEntity identity)
            entity0 = identity;

        if (entity0 is EntityUid entity)
        {
            if (_entity.TryGetComponent(entity, out GrammarComponent? grammar) && grammar.Gender.HasValue)
            {
                return new LocValueString(grammar.Gender.Value.ToString().ToLowerInvariant());
            }

            if (TryGetEntityLocAttrib(entity, "gender", out var gender))
            {
                return new LocValueString(gender);
            }
        }

        return new LocValueString(nameof(Gender.Neuter));
    }

    private ILocValue FuncReflexive(LocArgs args)
    {
        var arg = args.Args[0];
        if (arg.Value is IdentityEntity identity)
            arg = new LocValueEntity(identity.Entity);

        return new LocValueString(_loc.GetString("zzzz-reflexive-pronoun", ("ent", arg)));
    }

    private ILocValue FuncProper(LocArgs args)
    {
        if (args.Args.Count < 1) return new LocValueString("false");

        var entity0 = args.Args[0].Value;
        if (entity0 is IdentityEntity identity)
            entity0 = identity;

        if (entity0 is EntityUid entity)
        {
            if (_entity.TryGetComponent(entity, out GrammarComponent? grammar) && grammar.ProperNoun.HasValue)
            {
                return new LocValueString(grammar.ProperNoun.Value.ToString().ToLowerInvariant());
            }

            if (TryGetEntityLocAttrib(entity, "proper", out var proper))
            {
                return new LocValueString(proper);
            }
        }

        return new LocValueString("false");
    }

    private bool TryGetEntityLocAttrib(EntityUid entity, string attribute, [NotNullWhen(true)] out string? value)
    {
        if (_entity.TryGetComponent(entity, out GrammarComponent? grammar) &&
            grammar.Attributes.TryGetValue(attribute, out value))
        {
            return true;
        }

        if (_entity.GetComponent<MetaDataComponent>(entity).EntityPrototype is not {} prototype)
        {
            value = null;
            return false;
        }

        var data = _loc.GetEntityData(prototype.ID);
        return data.Attributes.TryGetValue(attribute, out value);
    }
}
