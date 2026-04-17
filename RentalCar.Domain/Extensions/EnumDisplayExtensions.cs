using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace RentalCar.Domain.Extensions;

public static class EnumDisplayExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value);
        if (name is not null)
        {
            var member = type.GetField(name);
            var display = member?.GetCustomAttribute<DisplayAttribute>();
            if (!string.IsNullOrEmpty(display?.Name))
                return display.Name!;
            return name;
        }

        return value.ToString();
    }
}
