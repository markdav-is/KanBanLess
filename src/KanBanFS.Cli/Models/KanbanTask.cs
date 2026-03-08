namespace KanBanFS.Cli.Models;

public class KanbanTask
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Created { get; set; } = string.Empty;
    public string Updated { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium";
    public List<string> Tags { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public List<ChecklistItem> Checklist { get; set; } = new();
    public string Column { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}

public class ChecklistItem
{
    public bool Completed { get; set; }
    public string Text { get; set; } = string.Empty;
}
