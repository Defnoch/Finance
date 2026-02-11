using System.Text.RegularExpressions;

namespace Finance.Categorization;

public static class Normalizer
{
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var text = input.Trim().ToUpperInvariant();
        text = Regex.Replace(text, "\\s+", " ");
        return text;
    }
}

