namespace Content.Shared._RMC14.Xenonids.Parasite;

[ByRefEvent]
public record struct GetInfectedIncubationMultiplierEvent()
{
    public List<float> Additions = new();
    public List<float> Multipliers = new();

    public void Add(float multiplier)
    {
        Additions.Add(multiplier);
    }

    public void Multiply(float multiplier)
    {
        Multipliers.Add(multiplier);
    }
}
