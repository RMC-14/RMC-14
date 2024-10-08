using JetBrains.Annotations;

namespace Content.Shared._RMC14.Weapons.Ranged.Whitelist;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
[BaseTypeRequired(typeof(IComponent))]
public sealed class SpecialistSkillComponentAttribute(string name) : Attribute
{
    public readonly string Name = name;
}
