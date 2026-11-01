using System.Globalization;

namespace Material_Tracker.Utilities;

public static class PersianDate
{
    public static string Format(DateOnly date)
    {
        var calendar = new PersianCalendar();
        var value = date.ToDateTime(TimeOnly.MinValue);
        return PersianText.Digits(FormattableString.Invariant(
            $"{calendar.GetYear(value):0000}/{calendar.GetMonth(value):00}/{calendar.GetDayOfMonth(value):00}"));
    }

    public static DateOnly Parse(string text)
    {
        var parts = PersianText.LatinDigits(text).Split('/');
        if (parts.Length != 3 || parts[0].Length != 4 ||
            !parts.All(part => part.Length > 0 && part.All(char.IsAsciiDigit)) ||
            !int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var month) || !int.TryParse(parts[2], out var day))
            throw new Services.SitePackDataException("تاریخ شمسی را به صورت سال/ماه/روز وارد کنید.");
        try
        {
            return DateOnly.FromDateTime(new PersianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0));
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new Services.SitePackDataException("تاریخ شمسی معتبر نیست. روز، ماه و سال را بررسی کنید.");
        }
    }
}
