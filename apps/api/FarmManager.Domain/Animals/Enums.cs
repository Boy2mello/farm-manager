namespace FarmManager.Domain.Animals;

public enum AnimalSex
{
    Female = 1,
    Male = 2,
}

public enum AnimalStatus
{
    Active = 1,
    Open = 2,
    Exposed = 3,
    PregnantConfirmed = 4,
    Lactating = 5,
    Dry = 6,
    Sold = 7,
    Dead = 8,
    Missing = 9,
    Transferred = 10,
}

public enum AnimalSource
{
    BornOnFarm = 1,
    Purchased = 2,
    Inherited = 3,
    TransferredIn = 4,
    Legacy = 5,
}

public enum DobPrecision
{
    Day = 1,
    Month = 2,
    Year = 3,
}

public enum PerformanceTier
{
    None = 0,
    A = 1,
    B = 2,
    C = 3,
    D = 4,
    E = 5,
}
