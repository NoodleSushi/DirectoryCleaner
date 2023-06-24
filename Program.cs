using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;
using DirectoryCleaner;

const string HELP_TEXT = @"
USAGE:
    DirectoryCleaner CONFIG_PATH [FLAGS]
    DirectoryCleaner -h
    DirectoryCleaner --help

CONFIG JSON FORMAT:
{
    ""rules"": [
        {
            [""label"": string,]    Label of the directory
            ""dir"": string,        Directory path
            [""rgx"": string,]      RegEx pattern
            [""flags"": string,]    Flags string separated by space
        },
        ...
    ]
}

FLAGS:
-f      --forced
-r      --recursive
-df     --delete-folder
-du     --delete-unzipped
";

if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
{
    Console.WriteLine(HELP_TEXT);
    return;
}

if (GetFileInfo(args[0]) is not FileInfo jsonTextDir)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"CONFIG PATH {args[0]} DOES NOT EXIST");
    Console.ResetColor();
    return;
}

string jsonText = File.ReadAllText(jsonTextDir.FullName);

if (JsonConvert.DeserializeObject<DirectorySettingJson>(jsonText) is not DirectorySettingJson jsonSettings)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"CONFIG DESERIALIZE ERROR");
    Console.ResetColor();
    return;
}

var rules = jsonSettings.Rules
    .Select(x => new DirectorySettingRule(x))
    .OrderByDescending((x) => x.Flags & DirectoryFlags.Forced);

foreach (DirectorySettingRule rule in rules)
{
    if (GetDirectoryInfo(rule.Setting.Directory) is not DirectoryInfo directoryInfo)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"DIRECTORY \"{rule.Setting.Directory}\" DOES NOT EXIST");
        Console.WriteLine("SKIPPING");
        Console.ResetColor();
        continue;
    }
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(rule.Setting.Label ?? directoryInfo.FullName);
    Console.ResetColor();
    bool shallDelete = rule.Flags.HasFlag(DirectoryFlags.Forced);
    bool decided = shallDelete;
    if (!decided)
    {
        string logs = TraverseDirectorySetting(rule);
        if (string.IsNullOrEmpty(logs))
        {
            decided = true;
            shallDelete = true;
        }
        else
        {
            Console.WriteLine("The following files and directories will be deleted:");
            Console.WriteLine(logs);
            Console.WriteLine(
                "type 'open' to open directory in file explorer\n" +
                "type 'yes' to start deletion\n" +
                "type 'no' to skip deletion:"
            );
        }
        while (!decided)
        {
            string? inp = Console.ReadLine();
            switch (inp)
            {
                case "open":
                    Process.Start("explorer.exe", directoryInfo.FullName);
                    break;
                case "yes":
                    shallDelete = true;
                    decided = true;
                    break;
                case "no":
                    shallDelete = false;
                    decided = true;
                    break;
            }
        }
    }
    Console.WriteLine();
    if (shallDelete)
    {
        Console.WriteLine("Deleting contents");
        string logs = TraverseDirectorySetting(rule, true);
        Console.Write(logs);
    }
    else
        Console.WriteLine("Skipped deleting contents");
    Console.WriteLine();
    Console.WriteLine();
}

static DirectoryInfo? GetDirectoryInfo(string directoryPath)
{
    DirectoryInfo? directoryInfo = null;

    if (Path.IsPathRooted(directoryPath))
    {
        if (Directory.Exists(directoryPath))
        {
            directoryInfo = new DirectoryInfo(directoryPath);
        }
    }
    else
    {
        string currentDirectory = Environment.CurrentDirectory;
        string fullPath = Path.Combine(currentDirectory, directoryPath);

        if (Directory.Exists(fullPath))
        {
            directoryInfo = new DirectoryInfo(fullPath);
        }
    }

    return directoryInfo;
}

static FileInfo? GetFileInfo(string filePath)
{
    FileInfo? fileInfo = null;

    if (Path.IsPathRooted(filePath))
    {
        if (File.Exists(filePath))
        {
            fileInfo = new FileInfo(filePath);
        }
    }
    else
    {
        string currentDirectory = Environment.CurrentDirectory;
        string fullPath = Path.Combine(currentDirectory, filePath);

        if (File.Exists(fullPath))
        {
            fileInfo = new FileInfo(fullPath);
        }
    }

    return fileInfo;
}

static string TraverseDirectorySetting(DirectorySettingRule rule, bool delete = false)
{
    HashSet<string> ARCHIVE_EXTENSIONS = new() { ".zip", ".rar", ".7z" };

    if (GetDirectoryInfo(rule.Setting.Directory) is not DirectoryInfo root)
        return "";

    string logs = "";
    Action<FileSystemInfo> affectPath;

    if (delete)
        affectPath = (path) => {
            path.Delete();
            logs += $"Deleted: \"{path.FullName}\"\n";
        };
    else
        affectPath = (path) =>
            logs += $"{path.FullName}\n";

    bool hasRegEx = !string.IsNullOrEmpty(rule.Setting.RegExPattern);

    Func<FileSystemInfo, bool> evalPath = 
        x => !hasRegEx || Regex.IsMatch(x.FullName, rule.Setting.RegExPattern ?? "");

    Stack<(DirectoryInfo baseDir, bool delete, Queue<DirectoryInfo>? dirQueue)> dirLevels = new();

    dirLevels.Push(new(root, false, null));

    while (dirLevels.Count > 0)
    {
        var dirLevel = dirLevels.Pop();
        if (dirLevel.dirQueue == null)
        {
            var files = dirLevel.baseDir.EnumerateFiles().Where(x => dirLevel.delete || evalPath(x));
            
            foreach (var file in files)
            {
                affectPath(file);
            }

            dirLevel.dirQueue = new();

            foreach (var dir in dirLevel.baseDir.EnumerateDirectories())
            {
                dirLevel.dirQueue.Enqueue(dir);

                if ((dirLevels.Count == 0 || rule.Flags.HasFlag(DirectoryFlags.Recursive)) && rule.Flags.HasFlag(DirectoryFlags.DeleteUnzipped))
                {
                    FileInfo? archive = dirLevel.baseDir.EnumerateFiles($"{dir.Name}*", SearchOption.TopDirectoryOnly)
                        .Where(x => ARCHIVE_EXTENSIONS.Contains(x.Extension))
                        .FirstOrDefault();
                    if (archive is not null)
                    {
                        affectPath(archive);
                    }
                }
            }
            dirLevels.Push(dirLevel);
        }
        else
        {
            if (dirLevel.dirQueue.Count > 0)
            {
                var dirElem = dirLevel.dirQueue.Dequeue();
                dirLevels.Push(dirLevel);

                bool shallDelete = evalPath(dirElem);
                bool isRecursive = rule.Flags.HasFlag(DirectoryFlags.Recursive);
                if (shallDelete || isRecursive)
                    dirLevels.Push(new(dirElem, shallDelete, null));
            }
            else
            {
                bool isRoot = dirLevel.baseDir == root;
                bool isRootDeletable = rule.Flags.HasFlag(DirectoryFlags.DeleteFolder);
                if ((!isRoot || isRootDeletable) && (dirLevel.delete || evalPath(dirLevel.baseDir)))
                    affectPath(dirLevel.baseDir);
            }
        }
    }

    return logs;
}
