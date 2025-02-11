using OnecMonitor.Common.DTO;

namespace OnecMonitor.Common.Extensions;

public static class ListExtensions
{
    public static void AddLogItem(this List<UpdateInfoBaseTaskResultLogItemDto> list, string message,
        bool isError = false)
    {
        list.Add(new UpdateInfoBaseTaskResultLogItemDto
        {
            TimeStamp = DateTime.Now,
            IsError = isError,
            Message = message
        });
    }
}