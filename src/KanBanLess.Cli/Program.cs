using System.Text.RegularExpressions;

string[] columns = ["backlog", "todo", "doing", "done"];

if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
{
    PrintUsage();
    return 0;
}

return args[0] switch
{
    "init"   => Init(args.Length > 1 ? string.Join(" ", args[1..]) : "kanban"),
    "add"    => Add(string.Join(" ", args[1..])),
    "move"   => Move(args.ElementAtOrDefault(1), args.ElementAtOrDefault(2)),
    "list"   => List(args.Length > 1 ? args[1..] : []),
    "show"   => Show(args.ElementAtOrDefault(1)),
    "check"  => Check(args.ElementAtOrDefault(1), string.Join(" ", args[2..])),
    "status" => Status(),
    _        => Unknown(args[0]),
};

// ---------------------------------------------------------------------------
// Commands
// ---------------------------------------------------------------------------

int Init(string name)
{
    var slug  = Slugify(name);
    var board = slug;
    var n     = 1;
    while (Directory.Exists(board))
        board = $"{slug}-{n++}";

    foreach (var col in columns)
    {
        Directory.CreateDirectory(Path.Combine(board, col));
        Console.WriteLine($"  created {board}/{col}/");
    }
    Console.WriteLine($"Board initialised: {board}/");
    Console.WriteLine($"  → cd {board}");
    return 0;
}

int Add(string title)
{
    if (string.IsNullOrWhiteSpace(title))
    {
        Console.Error.WriteLine("Usage: kanban add <title>");
        return 1;
    }
    if (!Directory.Exists("backlog"))
    {
        Console.Error.WriteLine("Error: backlog/ not found. Run 'kanban init' first.");
        return 1;
    }
    var slug = Slugify(title);
    if (string.IsNullOrWhiteSpace(slug))
    {
        Console.Error.WriteLine("Error: could not generate a valid slug from title. Please include letters or numbers in the title.");
        return 1;
    }
    var file = Path.Combine("backlog", $"{slug}.md");
    if (File.Exists(file))
    {
        Console.Error.WriteLine($"Error: task already exists: {file}");
        return 1;
    }
    File.WriteAllText(file, $"""
        ---
        priority: medium
        tags: []
        ---

        # {title}

        Brief description of the task.

        ## Checklist

        - [ ] Step one
        - [ ] Step two
        - [ ] Step three

        """);
    Console.WriteLine($"Created: {file}");
    return 0;
}

int Move(string? task, string? column)
{
    if (string.IsNullOrEmpty(task) || string.IsNullOrEmpty(column))
    {
        Console.Error.WriteLine("Usage: kanban move <task> <column>");
        return 1;
    }
    if (!ValidateSlug(task)) return 1;
    if (!ValidateColumn(column)) return 1;
    var src = FindTask(task);
    if (src is null) return 1;
    if (!Directory.Exists(column))
    {
        Console.Error.WriteLine($"Error: column '{column}/' not found. Run 'kanban init' first.");
        return 1;
    }
    var dest = Path.Combine(column, $"{task}.md");
    if (File.Exists(dest))
    {
        Console.Error.WriteLine($"Error: task already exists in column '{column}': {dest}");
        return 1;
    }
    try
    {
        File.Move(src, dest);
    }
    catch (IOException ex)
    {
        Console.Error.WriteLine($"Error: failed to move task '{task}' to column '{column}': {ex.Message}");
        return 1;
    }
    Console.WriteLine($"Moved: {task}  →  {column}");
    return 0;
}

int List(string[] listArgs)
{
    string? column         = null;
    string? priorityFilter = null;
    string? tagFilter      = null;

    for (int i = 0; i < listArgs.Length; i++)
    {
        if (listArgs[i] == "--priority" && i + 1 < listArgs.Length)
            priorityFilter = listArgs[++i].ToLowerInvariant();
        else if (listArgs[i] == "--tag" && i + 1 < listArgs.Length)
            tagFilter = listArgs[++i].ToLowerInvariant();
        else if (!listArgs[i].StartsWith("--"))
            column = listArgs[i];
    }

    if (column is not null && !ValidateColumn(column)) return 1;

    if (column is not null)
        PrintColumn(column, priorityFilter, tagFilter);
    else
        foreach (var col in columns)
            PrintColumn(col, priorityFilter, tagFilter);

    return 0;
}

int Show(string? task)
{
    if (string.IsNullOrEmpty(task))
    {
        Console.Error.WriteLine("Usage: kanban show <task>");
        return 1;
    }
    if (!ValidateSlug(task)) return 1;
    var file = FindTask(task);
    if (file is null) return 1;
    Console.Write(File.ReadAllText(file));
    return 0;
}

int Check(string? task, string item)
{
    if (string.IsNullOrEmpty(task) || string.IsNullOrEmpty(item))
    {
        Console.Error.WriteLine("Usage: kanban check <task> <item>");
        return 1;
    }
    if (!ValidateSlug(task)) return 1;
    var file = FindTask(task);
    if (file is null) return 1;

    var content    = File.ReadAllText(file);
    var unchecked_ = $"- [ ] {item}";
    var checked_   = $"- [x] {item}";

    if (content.Contains(unchecked_))
    {
        var index = content.IndexOf(unchecked_, StringComparison.Ordinal);
        if (index >= 0)
        {
            var newContent = content[..index] + checked_ + content[(index + unchecked_.Length)..];
            File.WriteAllText(file, newContent);
            Console.WriteLine($"Checked: \"{item}\" in {task}");
        }
    }
    else if (content.Contains(checked_))
    {
        Console.WriteLine($"Already checked: \"{item}\" in {task}");
    }
    else
    {
        Console.Error.WriteLine($"Error: checklist item not found: \"{item}\"");
        return 1;
    }
    return 0;
}

int Status()
{
    Console.WriteLine("Board Status");
    Console.WriteLine("============");
    var total = 0;
    foreach (var col in columns)
    {
        var count = Directory.Exists(col)
            ? Directory.GetFiles(col, "*.md").Length
            : 0;
        Console.WriteLine($"  {col,-10} {count}");
        total += count;
    }
    Console.WriteLine("------------");
    Console.WriteLine($"  {"total",-10} {total}");
    return 0;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

TaskInfo ReadTaskInfo(string filePath)
{
    var slug     = Path.GetFileNameWithoutExtension(filePath);
    var priority = "medium";
    var tags     = Array.Empty<string>();

    var lines     = File.ReadAllLines(filePath);
    var inFm      = false;
    var dashCount = 0;

    foreach (var line in lines)
    {
        if (line.Trim() == "---")
        {
            dashCount++;
            inFm = dashCount == 1;
            if (dashCount == 2) break;
            continue;
        }
        if (!inFm) continue;

        if (line.StartsWith("priority:"))
            priority = line["priority:".Length..].Trim().ToLowerInvariant();
        else if (line.StartsWith("tags:"))
        {
            var part = line["tags:".Length..].Trim();
            if (part.StartsWith("[") && part.EndsWith("]"))
                tags = part[1..^1]
                    .Split(',')
                    .Select(t => t.Trim().ToLowerInvariant())
                    .Where(t => t.Length > 0)
                    .ToArray();
        }
    }
    return new TaskInfo(slug, priority, tags);
}

int PriorityOrder(string p) => p switch { "high" => 0, "medium" => 1, "low" => 2, _ => 3 };

void PrintColumn(string col, string? priorityFilter, string? tagFilter)
{
    Console.WriteLine($"## {col}");
    if (!Directory.Exists(col))
    {
        Console.WriteLine("  (missing)");
        return;
    }

    var tasks = Directory.GetFiles(col, "*.md")
        .Select(ReadTaskInfo)
        .Where(t => priorityFilter is null || t.Priority == priorityFilter)
        .Where(t => tagFilter is null || t.Tags.Contains(tagFilter))
        .OrderBy(t => PriorityOrder(t.Priority))
        .ThenBy(t => t.Slug)
        .ToArray();

    if (tasks.Length == 0)
    {
        var label = priorityFilter is not null || tagFilter is not null ? "(no matches)" : "(empty)";
        Console.WriteLine($"  {label}");
    }
    else
    {
        foreach (var task in tasks)
        {
            var tagStr = task.Tags.Length > 0
                ? "  " + string.Join(" ", task.Tags.Select(t => $"#{t}"))
                : "";
            Console.WriteLine($"  - {task.Slug}  [{task.Priority}]{tagStr}");
        }
    }
}

string? FindTask(string slug)
{
    foreach (var col in columns)
    {
        var path = Path.Combine(col, $"{slug}.md");
        if (File.Exists(path)) return path;
    }
    Console.Error.WriteLine($"Error: task not found: {slug}");
    return null;
}

bool ValidateColumn(string col)
{
    if (columns.Contains(col)) return true;
    Console.Error.WriteLine($"Error: invalid column '{col}'. Must be one of: {string.Join(", ", columns)}");
    return false;
}

bool ValidateSlug(string slug)
{
    if (string.IsNullOrEmpty(slug))
    {
        Console.Error.WriteLine("Error: task name must not be empty.");
        return false;
    }
    const string SlugPattern = @"^[a-z0-9-]+$";
    if (Regex.IsMatch(slug, SlugPattern)) return true;
    Console.Error.WriteLine($"Error: invalid task name '{slug}'. Task names must contain only lowercase letters, digits, and hyphens.");
    return false;
}

string Slugify(string input) =>
    Regex.Replace(
        Regex.Replace(input.ToLowerInvariant(), @"[^a-z0-9]+", "-"),
        @"^-|-$", "");

int Unknown(string cmd)
{
    Console.Error.WriteLine($"Error: unknown command '{cmd}'");
    Console.Error.WriteLine();
    PrintUsage(Console.Error);
    return 1;
}

void PrintUsage(TextWriter? writer = null)
{
    writer ??= Console.Out;
    writer.WriteLine("""
        Usage: kanban <command> [args]

        Commands:
          init [name]             Create a board folder with 4 column directories.
                                  Defaults to 'kanban'; auto-increments if it exists
                                  (kanban-1, kanban-2, …). cd into the folder to use
                                  other commands.
          add <title>             Create a new task .md in backlog/
          move <task> <column>    Move a task file to a new column directory
          list [column]           List tasks; sorted high→medium→low by default
            [--priority high|medium|low]  Filter by priority
            [--tag <tag>]                 Filter by tag
          show <task>             Display a task's content
          check <task> <item>     Mark a checklist item complete
          status                  Show board summary (count per column)

        Columns: backlog, todo, doing, done
        """);
}

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

record TaskInfo(string Slug, string Priority, string[] Tags);
