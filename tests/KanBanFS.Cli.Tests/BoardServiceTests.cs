using KanBanFS.Cli.Services;

namespace KanBanFS.Cli.Tests;

public class BoardServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly BoardService _service;

    public BoardServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "kanbanfs-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
        _service = new BoardService(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Init_CreatesAllFourColumnDirectories()
    {
        _service.Init();

        Assert.True(Directory.Exists(Path.Combine(_tempDir, "backlog")));
        Assert.True(Directory.Exists(Path.Combine(_tempDir, "todo")));
        Assert.True(Directory.Exists(Path.Combine(_tempDir, "doing")));
        Assert.True(Directory.Exists(Path.Combine(_tempDir, "done")));
    }

    [Fact]
    public void IsBoardInitialized_ReturnsFalseWhenNotInitialized()
    {
        Assert.False(_service.IsBoardInitialized());
    }

    [Fact]
    public void IsBoardInitialized_ReturnsTrueAfterInit()
    {
        _service.Init();
        Assert.True(_service.IsBoardInitialized());
    }

    [Fact]
    public void Add_CreatesTaskFileInBacklog()
    {
        _service.Init();
        var task = _service.Add("My New Task");

        Assert.Equal("my-new-task", task.Id);
        Assert.Equal("My New Task", task.Title);
        Assert.Equal("backlog", task.Column);
        Assert.True(File.Exists(Path.Combine(_tempDir, "backlog", "my-new-task.md")));
    }

    [Fact]
    public void Add_TaskFileContainsYamlFrontmatter()
    {
        _service.Init();
        _service.Add("Test Task", priority: "high");

        var content = File.ReadAllText(Path.Combine(_tempDir, "backlog", "test-task.md"));
        Assert.Contains("id: test-task", content);
        Assert.Contains("title: Test Task", content);
        Assert.Contains("priority: high", content);
        Assert.Contains("---", content);
    }

    [Fact]
    public void Add_TaskFileContainsChecklist()
    {
        _service.Init();
        _service.Add("Checklist Task");

        var content = File.ReadAllText(Path.Combine(_tempDir, "backlog", "checklist-task.md"));
        Assert.Contains("## Checklist", content);
        Assert.Contains("- [ ] Step one", content);
        Assert.Contains("- [ ] Step two", content);
        Assert.Contains("- [ ] Step three", content);
    }

    [Fact]
    public void Move_MovesTaskToTargetColumn()
    {
        _service.Init();
        _service.Add("Move Me");

        var result = _service.Move("move-me", "todo");

        Assert.Equal("todo", result!.Column);
        Assert.True(File.Exists(Path.Combine(_tempDir, "todo", "move-me.md")));
        Assert.False(File.Exists(Path.Combine(_tempDir, "backlog", "move-me.md")));
    }

    [Fact]
    public void Move_ThrowsWhenTaskNotFound()
    {
        _service.Init();
        Assert.Throws<FileNotFoundException>(() => _service.Move("nonexistent-task", "todo"));
    }

    [Fact]
    public void Move_ThrowsWhenColumnIsInvalid()
    {
        _service.Init();
        _service.Add("Test Task");
        Assert.Throws<ArgumentException>(() => _service.Move("test-task", "invalid-column"));
    }

    [Fact]
    public void List_ReturnsAllTasksAcrossColumns()
    {
        _service.Init();
        _service.Add("Task One");
        _service.Add("Task Two");
        _service.Move("task-one", "doing");

        var tasks = _service.List().ToList();

        Assert.Equal(2, tasks.Count);
        Assert.Contains(tasks, t => t.Id == "task-one" && t.Column == "doing");
        Assert.Contains(tasks, t => t.Id == "task-two" && t.Column == "backlog");
    }

    [Fact]
    public void List_FiltersByColumn()
    {
        _service.Init();
        _service.Add("Backlog Task");
        _service.Add("Todo Task");
        _service.Move("todo-task", "todo");

        var backlogTasks = _service.List("backlog").ToList();
        Assert.Single(backlogTasks);
        Assert.Equal("backlog-task", backlogTasks[0].Id);
    }

    [Fact]
    public void Show_ReturnsTaskBySlug()
    {
        _service.Init();
        _service.Add("Show Task");

        var result = _service.Show("show-task");

        Assert.NotNull(result);
        Assert.Equal("show-task", result.Id);
        Assert.Equal("Show Task", result.Title);
    }

    [Fact]
    public void Show_ReturnsNullForNonexistentTask()
    {
        _service.Init();
        var result = _service.Show("no-such-task");
        Assert.Null(result);
    }

    [Fact]
    public void Check_MarksChecklistItemComplete()
    {
        _service.Init();
        _service.Add("Check Task");

        var result = _service.Check("check-task", "Step one");

        Assert.NotNull(result);
        var item = result.Checklist.First(i => i.Text == "Step one");
        Assert.True(item.Completed);

        // Verify persistence
        var reloaded = _service.Show("check-task");
        var reloadedItem = reloaded!.Checklist.First(i => i.Text == "Step one");
        Assert.True(reloadedItem.Completed);
    }

    [Fact]
    public void Check_ThrowsWhenTaskNotFound()
    {
        _service.Init();
        Assert.Throws<FileNotFoundException>(() => _service.Check("no-such-task", "step one"));
    }

    [Fact]
    public void Check_ThrowsWhenItemNotFound()
    {
        _service.Init();
        _service.Add("Check Task");
        Assert.Throws<ArgumentException>(() => _service.Check("check-task", "nonexistent item"));
    }

    [Fact]
    public void Status_ReturnsCountPerColumn()
    {
        _service.Init();
        _service.Add("Task One");
        _service.Add("Task Two");
        _service.Move("task-one", "doing");

        var counts = _service.Status();

        Assert.Equal(1, counts["backlog"]);
        Assert.Equal(0, counts["todo"]);
        Assert.Equal(1, counts["doing"]);
        Assert.Equal(0, counts["done"]);
    }

    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("My Task #1", "my-task-1")]
    [InlineData("  Spaces  ", "spaces")]
    [InlineData("Special@#$Chars", "specialchars")]
    [InlineData("Multiple   Spaces", "multiple-spaces")]
    public void ToSlug_GeneratesExpectedSlug(string title, string expected)
    {
        Assert.Equal(expected, BoardService.ToSlug(title));
    }

    [Fact]
    public void ParseTaskContent_ParsesFrontmatterCorrectly()
    {
        var content = """
            ---
            id: my-task
            title: My Task
            created: 2024-01-01
            updated: 2024-01-02
            priority: high
            tags: [bug, urgent]
            ---

            ## Description

            A test task.

            ## Checklist

            - [x] Step one
            - [ ] Step two
            """;

        var task = BoardService.ParseTaskContent(content, "backlog/my-task.md", "backlog");

        Assert.NotNull(task);
        Assert.Equal("my-task", task.Id);
        Assert.Equal("My Task", task.Title);
        Assert.Equal("high", task.Priority);
        Assert.Equal(new[] { "bug", "urgent" }, task.Tags);
        Assert.Equal("A test task.", task.Description);
        Assert.Equal(2, task.Checklist.Count);
        Assert.True(task.Checklist[0].Completed);
        Assert.False(task.Checklist[1].Completed);
    }
}
