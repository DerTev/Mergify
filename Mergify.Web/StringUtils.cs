using System.Text.RegularExpressions;

namespace Mergify.Web;

public static class StringUtils
{
    public static Dictionary<string, string> ToDictionary(this string input, string pattern)
    {
        var split = Regex.Split(input.Replace("?", ""), pattern);
        return split.Where((_, i) => i % 2 == 0).Zip(split.Where((_, i) => i % 2 != 0)).ToDictionary();
    }
}
