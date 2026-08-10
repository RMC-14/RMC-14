namespace Content.Server._RMC14.Stations;

[DataRecord]
public readonly partial record struct JobSlotScaling(
    int Factor,
    int C,
    int Min,
    int Max,
    bool Squad,
    int MinimumPlayers = 0);
