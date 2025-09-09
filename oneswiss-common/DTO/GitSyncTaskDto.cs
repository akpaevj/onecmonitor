using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.Common.DTO;

[ContextClass("ЗадачаСинхронизацииХранилища", "GitSyncTask")]
[MessagePackObject]
public class GitSyncTaskDto
{
    [ContextProperty("Идентификатор", "Id")]
    [Key(0)]
    public Guid Id { get; set; }

    [ContextProperty("Активна", "IsActive")]
    [Key(2)]
    public bool IsActive { get; set; }

    [ContextProperty("ИмяВетки", "BranchName")]
    [Key(3)]
    public string BranchName { get; set; }

    [ContextProperty("РепозиторийGit", "GitRepository")]
    [Key(4)]
    public GitRepositoryDto GitRepository { get; set; }

    [ContextProperty("Элементы", "Elements")]
    [Key(5)]
    public List<GitSyncTaskItemDto> Items { get; set; } = [];
}