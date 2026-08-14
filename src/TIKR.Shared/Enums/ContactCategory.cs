namespace TIKR.Shared.Enums;

[Flags]
public enum ContactCategory
{
    None = 0,
    Budget = 1,
    Audit = 2,
    MillLevy = 4,
    Compliance = 8,
    Election = 16,
    Custom = 32
}
