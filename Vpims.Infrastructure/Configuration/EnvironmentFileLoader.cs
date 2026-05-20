namespace Vpims.Infrastructure.Configuration;

public static class EnvironmentFileLoader
{
    private const string EnvironmentFileName = ".env";

    public static void Load(params string[] candidateDirectories)
    {
        foreach (string directory in ResolveDirectories(candidateDirectories))
        {
            string environmentFilePath = Path.Combine(directory, EnvironmentFileName);

            if (!File.Exists(environmentFilePath))
            {
                continue;
            }

            foreach (string line in File.ReadLines(environmentFilePath))
            {
                LoadLine(line);
            }
        }
    }

    private static IEnumerable<string> ResolveDirectories(IEnumerable<string> candidateDirectories)
    {
        var seenDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string? candidateDirectory in candidateDirectories.Append(Directory.GetCurrentDirectory()))
        {
            if (string.IsNullOrWhiteSpace(candidateDirectory))
            {
                continue;
            }

            IReadOnlyList<DirectoryInfo> directoryChain = BuildDirectoryChain(candidateDirectory);

            foreach (DirectoryInfo directory in directoryChain)
            {
                if (seenDirectories.Add(directory.FullName))
                {
                    yield return directory.FullName;
                }
            }
        }
    }

    private static IReadOnlyList<DirectoryInfo> BuildDirectoryChain(string candidateDirectory)
    {
        var directories = new List<DirectoryInfo>();
        DirectoryInfo? directory = new DirectoryInfo(candidateDirectory);

        while (directory is not null)
        {
            directories.Add(directory);

            if (IsWorkspaceRoot(directory))
            {
                return directories;
            }

            directory = directory.Parent;
        }

        return directories.Count == 0 ? [] : [directories[0]];
    }

    private static bool IsWorkspaceRoot(DirectoryInfo directory)
    {
        return File.Exists(Path.Combine(directory.FullName, "package.json")) &&
            Directory.Exists(Path.Combine(directory.FullName, "backend"));
    }

    private static void LoadLine(string line)
    {
        string trimmedLine = line.Trim();

        if (trimmedLine.Length == 0 || trimmedLine.StartsWith('#'))
        {
            return;
        }

        int separatorIndex = trimmedLine.IndexOf('=');

        if (separatorIndex <= 0)
        {
            return;
        }

        string key = trimmedLine[..separatorIndex].Trim();
        string value = trimmedLine[(separatorIndex + 1)..].Trim();

        if (key.Length == 0 || Environment.GetEnvironmentVariable(key) is not null)
        {
            return;
        }

        Environment.SetEnvironmentVariable(key, Unquote(value));
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') ||
             (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }
}