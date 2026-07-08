using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;

namespace TIKR.Shared.Helpers;

public static class RequirementUrgencyHelper
{
    public static RequirementUrgency GetUrgency(RequirementDto requirement, DateOnly? referenceDate = null)
    {
        if (requirement.IsCompleted)
            return RequirementUrgency.Completed;

        var today = referenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var daysUntil = requirement.DueDate.DayNumber - today.DayNumber;

        if (daysUntil < 0)
            return RequirementUrgency.Overdue;
        if (daysUntil <= 14)
            return RequirementUrgency.High;
        if (daysUntil <= 30)
            return RequirementUrgency.Medium;

        return RequirementUrgency.Low;
    }

    public static string GetLabel(RequirementUrgency urgency) => urgency switch
    {
        RequirementUrgency.Overdue => "Overdue",
        RequirementUrgency.High => "High",
        RequirementUrgency.Medium => "Medium",
        RequirementUrgency.Low => "Low",
        RequirementUrgency.Completed => "Done",
        _ => "Low"
    };

    public static (byte R, byte G, byte B) GetTableColor(RequirementUrgency urgency) => urgency switch
    {
        RequirementUrgency.Overdue => (180, 35, 24),
        RequirementUrgency.High => (247, 144, 9),
        RequirementUrgency.Medium => (250, 204, 21),
        RequirementUrgency.Low => (18, 183, 106),
        RequirementUrgency.Completed => (152, 162, 179),
        _ => (152, 162, 179)
    };
}
