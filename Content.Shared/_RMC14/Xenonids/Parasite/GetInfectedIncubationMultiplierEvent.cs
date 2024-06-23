namespace Content.Shared._RMC14.Xenonids.Parasite;

[ByRefEvent]
public record struct GetInfectedIncubationMultiplierEvent(float Multiplier = 1)
{
    public void Add(float multiplier)
    {
        Multiplier += multiplier;
    }

    public void Multiply(float multiplier)
    {
        Multiplier *= multiplier;
    }
}
