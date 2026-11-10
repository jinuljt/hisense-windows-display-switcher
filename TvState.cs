namespace HisenseDisplaySwitcher;

internal readonly record struct TvState(bool Awake, int Source, int Port)
{
    public bool IsTarget(int targetPort) => Awake && Source == 4 && Port == targetPort;

    public static TvState Parse(string output)
    {
        var port = MatchInt(output, @"PORT=(\d+)");
        var source = MatchInt(output, @"SOURCE=(\d+)");
        return new(output.Contains("mWakefulness=Awake", StringComparison.OrdinalIgnoreCase), source, port);
    }

    private static int MatchInt(string input, string pattern)
    {
        var match = System.Text.RegularExpressions.Regex.Match(input, pattern);
        return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? value : -1;
    }
}

