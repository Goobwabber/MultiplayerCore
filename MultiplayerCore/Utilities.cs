namespace MultiplayerCore
{
    internal static class Utilities
    {
        internal static string? HashForLevelID(string? levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
                return null!;
            string[] ary = levelId!.Split('_', ' ');
            string hash = null!;
            if (ary.Length > 2)
                hash = ary[2];
            if ((hash?.Length ?? 0) == 40)
                return hash!;
            return null;
        }
    }
}
