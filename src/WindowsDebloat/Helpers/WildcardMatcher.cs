using System.Text.RegularExpressions;

namespace WindowsDebloat.Helpers;

public static class WildcardMatcher
{
	public static bool IsMatch(string value, string pattern)
	{
		var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
		return Regex.IsMatch(value, regexPattern, RegexOptions.IgnoreCase);
	}
}
