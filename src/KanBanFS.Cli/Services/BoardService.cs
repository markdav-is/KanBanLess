using System.Text;
using System.Text.RegularExpressions;
using KanBanFS.Cli.Models;

namespace KanBanFS.Cli.Services;

public class BoardService
{
    public static readonly string[] ValidColumns = { "backlog", "todo", "doing", "done" };

    private readonly string _boardPath;

    public BoardService(string boardPath)
    {
        _boardPath = boardPath;
    }

    public void Init()
    {
        foreach (var column in ValidColumns)
        {
            var dir = Path.Combine(_boardPath, column);
            Directory.CreateDirectory(dir);
        }
    }

    public bool IsBoardInitialized()
    {
        return ValidColumns.All(c => Directory.Exists(Path.Combine(_boardPath, c)));
    }

    public KanbanTask Add(string title, string priority = "medium", IEnumerable<string>? tags = null)
    {
        var id = ToSlug(title);
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var task = new KanbanTask
        {
            Id = id,
            Title = title,
            Created = today,
            Updated = today,
            Priority = priority,
            Tags = tags?.ToList() ?? new List<string>(),
            Description = "Brief description of the task.",
            Checklist = new List<ChecklistItem>
            {
                new() { Text = "Step one" },
                new() { Text = "Step two" },
                new() { Text = "Step three" }
            },
            Column = "backlog"
        };

        var filePath = Path.Combine(_boardPath, "backlog", $"{id}.md");
        WriteTaskFile(filePath, task);
        task.FilePath = filePath;
        return task;
    }

    public KanbanTask? Move(string taskName, string column)
    {
        column = column.ToLowerInvariant();
        if (!ValidColumns.Contains(column))
            throw new ArgumentException($"Invalid column '{column}'. Valid columns are: {string.Join(", ", ValidColumns)}");

        var (task, sourcePath) = FindTask(taskName)
            ?? throw new FileNotFoundException($"Task '{taskName}' not found.");

        var destDir = Path.Combine(_boardPath, column);
        var destPath = Path.Combine(destDir, Path.GetFileName(sourcePath));

        task.Column = column;
        task.Updated = DateTime.UtcNow.ToString("yyyy-MM-dd");
        WriteTaskFile(sourcePath, task);

        if (!string.Equals(sourcePath, destPath, StringComparison.OrdinalIgnoreCase))
            File.Move(sourcePath, destPath, overwrite: true);

        task.FilePath = destPath;
        return task;
    }

    public IEnumerable<KanbanTask> List(string? column = null)
    {
        var columns = column != null
            ? new[] { column.ToLowerInvariant() }
            : ValidColumns;

        foreach (var col in columns)
        {
            var dir = Path.Combine(_boardPath, col);
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.GetFiles(dir, "*.md").OrderBy(f => f))
            {
                var task = ReadTaskFile(file, col);
                if (task != null) yield return task;
            }
        }
    }

    public KanbanTask? Show(string taskName)
    {
        var result = FindTask(taskName);
        return result?.task;
    }

    public KanbanTask? Check(string taskName, string itemText)
    {
        var found = FindTask(taskName)
            ?? throw new FileNotFoundException($"Task '{taskName}' not found.");

        var (task, filePath) = found;

        var item = task.Checklist.FirstOrDefault(i =>
            i.Text.Contains(itemText, StringComparison.OrdinalIgnoreCase));

        if (item == null)
            throw new ArgumentException($"Checklist item containing '{itemText}' not found in task '{taskName}'.");

        item.Completed = true;
        task.Updated = DateTime.UtcNow.ToString("yyyy-MM-dd");
        WriteTaskFile(filePath, task);
        return task;
    }

    public Dictionary<string, int> Status()
    {
        var counts = new Dictionary<string, int>();
        foreach (var col in ValidColumns)
        {
            var dir = Path.Combine(_boardPath, col);
            counts[col] = Directory.Exists(dir)
                ? Directory.GetFiles(dir, "*.md").Length
                : 0;
        }
        return counts;
    }

    private (KanbanTask task, string filePath)? FindTask(string taskName)
    {
        // Normalize: strip .md extension if provided
        var slug = taskName.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            ? taskName[..^3]
            : taskName;

        foreach (var col in ValidColumns)
        {
            var dir = Path.Combine(_boardPath, col);
            if (!Directory.Exists(dir)) continue;

            // Try exact match first
            var exactPath = Path.Combine(dir, $"{slug}.md");
            if (File.Exists(exactPath))
            {
                var task = ReadTaskFile(exactPath, col);
                if (task != null) return (task, exactPath);
            }

            // Fuzzy match by filename
            foreach (var file in Directory.GetFiles(dir, "*.md"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (name.Contains(slug, StringComparison.OrdinalIgnoreCase))
                {
                    var task = ReadTaskFile(file, col);
                    if (task != null) return (task, file);
                }
            }
        }
        return null;
    }

    public static KanbanTask? ReadTaskFile(string filePath, string column)
    {
        if (!File.Exists(filePath)) return null;
        var content = File.ReadAllText(filePath);
        return ParseTaskContent(content, filePath, column);
    }

    public static KanbanTask? ParseTaskContent(string content, string filePath, string column)
    {
        var task = new KanbanTask
        {
            FilePath = filePath,
            Column = column
        };

        // Extract YAML frontmatter
        var frontmatterMatch = Regex.Match(content, @"^---\s*\n(.*?)\n---\s*\n", RegexOptions.Singleline);
        if (frontmatterMatch.Success)
        {
            var yaml = frontmatterMatch.Groups[1].Value;
            ParseFrontmatter(yaml, task);
        }

        // Extract description
        var descMatch = Regex.Match(content, @"## Description\s*\n+(.*?)(?=\n##|\z)", RegexOptions.Singleline);
        if (descMatch.Success)
            task.Description = descMatch.Groups[1].Value.Trim();

        // Extract checklist
        var checklistMatch = Regex.Match(content, @"## Checklist\s*\n+(.*?)(?=\n##|\z)", RegexOptions.Singleline);
        if (checklistMatch.Success)
        {
            var checklistText = checklistMatch.Groups[1].Value;
            var itemMatches = Regex.Matches(checklistText, @"- \[( |x)\] (.+)");
            foreach (Match m in itemMatches)
            {
                task.Checklist.Add(new ChecklistItem
                {
                    Completed = m.Groups[1].Value == "x",
                    Text = m.Groups[2].Value.Trim()
                });
            }
        }

        return task;
    }

    private static void ParseFrontmatter(string yaml, KanbanTask task)
    {
        foreach (var line in yaml.Split('\n'))
        {
            var colonIdx = line.IndexOf(':');
            if (colonIdx < 0) continue;
            var key = line[..colonIdx].Trim();
            var value = line[(colonIdx + 1)..].Trim();

            switch (key)
            {
                case "id": task.Id = value; break;
                case "title": task.Title = value; break;
                case "created": task.Created = value; break;
                case "updated": task.Updated = value; break;
                case "priority": task.Priority = value; break;
                case "tags":
                    if (value.StartsWith('[') && value.EndsWith(']'))
                    {
                        var inner = value[1..^1].Trim();
                        if (!string.IsNullOrWhiteSpace(inner))
                            task.Tags = inner.Split(',').Select(t => t.Trim()).ToList();
                    }
                    break;
            }
        }
    }

    private static void WriteTaskFile(string filePath, KanbanTask task)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"id: {task.Id}");
        sb.AppendLine($"title: {task.Title}");
        sb.AppendLine($"created: {task.Created}");
        sb.AppendLine($"updated: {task.Updated}");
        sb.AppendLine($"priority: {task.Priority}");
        sb.AppendLine($"tags: [{string.Join(", ", task.Tags)}]");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Description");
        sb.AppendLine();
        sb.AppendLine(task.Description);
        sb.AppendLine();
        sb.AppendLine("## Checklist");
        sb.AppendLine();
        foreach (var item in task.Checklist)
        {
            var mark = item.Completed ? "x" : " ";
            sb.AppendLine($"- [{mark}] {item.Text}");
        }

        File.WriteAllText(filePath, sb.ToString());
    }

    public static string ToSlug(string title)
    {
        var slug = title.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');
        return slug;
    }
}
