namespace Content.Shared._RMC14.Extensions;

public static class EnumExtensions
{
    private static class EnumInformation<T> where T : struct, Enum
    {
        // ReSharper disable once StaticMemberInGenericType
        internal static readonly T[] Values;

        static EnumInformation()
        {
            Values = Enum.GetValues<T>();
        }
    }

    public static T NextWrap<T>(this T en) where T : struct, Enum
    {
        var values = EnumInformation<T>.Values.AsSpan();
        if (values.Length == 0)
            return default;

        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            if (!EqualityComparer<T>.Default.Equals(en, value))
                continue;

            return values.Length > i + 1 ? values[i + 1] : values[0];
        }

        return values[0];
    }
}
