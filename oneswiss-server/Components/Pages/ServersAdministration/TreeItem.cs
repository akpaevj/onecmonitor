using MudBlazor;

namespace OneSwiss.Server.Components.Pages.ServersAdministration;

public class TreeItem(string id, TreeItem? parent = null)
{
    public string Id { get; set; } = id;
    public string Text { get; init; } = id;
    public string? AdornmentIcon { get; set; }
    public Color AdornmentIconColor { get; set; } = Color.Default;
    public TreeItem? Parent { get; set; } = parent;
    public Func<Task<List<TreeItem>>>? LoadChildrenFunc { get; init; }
    public List<TreeItem> Children { get; set; } = [];
    public bool Expanded { get; set; } = false;
    public bool Visible { get; set; } = true;
    public bool IsSelectable { get; set; } = true;
    public bool IsSelected { get; set; }
    public int Level
    {
        get
        {
            var level = 0;
            var parent = Parent;

            while (parent != null)
            {
                level++;
                parent = parent.Parent;
            }

            return level;
        }
    }
    public Type? DynamicComponentType { get; init; }
    public Dictionary<string, object>? Parameters { get; init; }
}