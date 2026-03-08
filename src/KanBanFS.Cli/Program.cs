using System.CommandLine;
using KanBanFS.Cli.Services;

var boardOption = new Option<string>(
    name: "--board",
    description: "Path to the board directory",
    getDefaultValue: () => Directory.GetCurrentDirectory());

var rootCommand = new RootCommand("KanBanFS — Kanban board in the file system");
rootCommand.AddGlobalOption(boardOption);

// kanban init
var initCommand = new Command("init", "Create the 4 column directories in the current folder");
initCommand.SetHandler((board) =>
{
    var service = new BoardService(board);
    service.Init();
    Console.WriteLine($"Board initialized at: {board}");
    Console.WriteLine("Columns created: backlog/, todo/, doing/, done/");
}, boardOption);

// kanban add <title>
var titleArg = new Argument<string>("title", "Task title");
var priorityOption = new Option<string>(
    name: "--priority",
    description: "Task priority",
    getDefaultValue: () => "medium");
priorityOption.FromAmong("low", "medium", "high");
var tagsOption = new Option<string[]>(
    name: "--tags",
    description: "Comma-separated tags")
{ AllowMultipleArgumentsPerToken = true };

var addCommand = new Command("add", "Create a new task in backlog/") { titleArg, priorityOption, tagsOption };
addCommand.SetHandler((board, title, priority, tags) =>
{
    var service = new BoardService(board);
    if (!service.IsBoardInitialized())
    {
        Console.Error.WriteLine("Board not initialized. Run 'kanban init' first.");
        Environment.Exit(1);
        return;
    }
    var task = service.Add(title, priority, tags);
    Console.WriteLine($"Created task: {task.Id}");
    Console.WriteLine($"  File: backlog/{task.Id}.md");
    Console.WriteLine($"  Title: {task.Title}");
    Console.WriteLine($"  Priority: {task.Priority}");
}, boardOption, titleArg, priorityOption, tagsOption);

// kanban move <task> <column>
var taskArg = new Argument<string>("task", "Task slug or filename");
var columnArg = new Argument<string>("column", "Target column (backlog, todo, doing, done)");

var moveCommand = new Command("move", "Move a task file to a new column directory") { taskArg, columnArg };
moveCommand.SetHandler((board, task, column) =>
{
    var service = new BoardService(board);
    try
    {
        var result = service.Move(task, column);
        if (result != null)
        {
            Console.WriteLine($"Moved '{result.Id}' to '{result.Column}'");
        }
    }
    catch (FileNotFoundException ex)
    {
        Console.Error.WriteLine(ex.Message);
        Environment.Exit(1);
    }
    catch (ArgumentException ex)
    {
        Console.Error.WriteLine(ex.Message);
        Environment.Exit(1);
    }
}, boardOption, taskArg, columnArg);

// kanban list [column]
var listColumnArg = new Argument<string?>("column", () => null, "Column to list (optional)");

var listCommand = new Command("list", "List tasks in one or all columns") { listColumnArg };
listCommand.SetHandler((board, column) =>
{
    var service = new BoardService(board);
    var tasks = service.List(column).ToList();

    if (!tasks.Any())
    {
        Console.WriteLine(column != null ? $"No tasks in '{column}'." : "No tasks found.");
        return;
    }

    string? currentCol = null;
    foreach (var t in tasks)
    {
        if (t.Column != currentCol)
        {
            currentCol = t.Column;
            Console.WriteLine($"\n[{currentCol}]");
        }
        Console.WriteLine($"  {t.Id} — {t.Title} [{t.Priority}]");
    }
}, boardOption, listColumnArg);

// kanban show <task>
var showTaskArg = new Argument<string>("task", "Task slug or filename");

var showCommand = new Command("show", "Display a task's content") { showTaskArg };
showCommand.SetHandler((board, task) =>
{
    var service = new BoardService(board);
    var result = service.Show(task);
    if (result == null)
    {
        Console.Error.WriteLine($"Task '{task}' not found.");
        Environment.Exit(1);
        return;
    }

    Console.WriteLine($"ID:       {result.Id}");
    Console.WriteLine($"Title:    {result.Title}");
    Console.WriteLine($"Column:   {result.Column}");
    Console.WriteLine($"Priority: {result.Priority}");
    Console.WriteLine($"Created:  {result.Created}");
    Console.WriteLine($"Updated:  {result.Updated}");
    if (result.Tags.Any())
        Console.WriteLine($"Tags:     {string.Join(", ", result.Tags)}");
    Console.WriteLine();
    Console.WriteLine($"Description:");
    Console.WriteLine($"  {result.Description}");
    Console.WriteLine();
    Console.WriteLine("Checklist:");
    foreach (var item in result.Checklist)
    {
        var mark = item.Completed ? "x" : " ";
        Console.WriteLine($"  [{mark}] {item.Text}");
    }
}, boardOption, showTaskArg);

// kanban check <task> <item>
var checkTaskArg = new Argument<string>("task", "Task slug or filename");
var checkItemArg = new Argument<string>("item", "Checklist item text (partial match)");

var checkCommand = new Command("check", "Mark a checklist item complete") { checkTaskArg, checkItemArg };
checkCommand.SetHandler((board, task, item) =>
{
    var service = new BoardService(board);
    try
    {
        var result = service.Check(task, item);
        if (result != null)
        {
            Console.WriteLine($"Checked off '{item}' in task '{result.Id}'.");
        }
    }
    catch (FileNotFoundException ex)
    {
        Console.Error.WriteLine(ex.Message);
        Environment.Exit(1);
    }
    catch (ArgumentException ex)
    {
        Console.Error.WriteLine(ex.Message);
        Environment.Exit(1);
    }
}, boardOption, checkTaskArg, checkItemArg);

// kanban status
var statusCommand = new Command("status", "Show board summary (count per column)");
statusCommand.SetHandler((board) =>
{
    var service = new BoardService(board);
    var counts = service.Status();
    var total = counts.Values.Sum();
    Console.WriteLine("Board Status:");
    foreach (var (col, count) in counts)
        Console.WriteLine($"  {col,-10} {count} task{(count == 1 ? "" : "s")}");
    Console.WriteLine($"  {"total",-10} {total} task{(total == 1 ? "" : "s")}");
}, boardOption);

rootCommand.AddCommand(initCommand);
rootCommand.AddCommand(addCommand);
rootCommand.AddCommand(moveCommand);
rootCommand.AddCommand(listCommand);
rootCommand.AddCommand(showCommand);
rootCommand.AddCommand(checkCommand);
rootCommand.AddCommand(statusCommand);

return await rootCommand.InvokeAsync(args);
