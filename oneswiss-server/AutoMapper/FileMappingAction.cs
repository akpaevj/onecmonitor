using AutoMapper;
using OneSwiss.Common.DTO;
using OneSwiss.Server.Services;
using File = OneSwiss.Server.Models.File;

namespace OneSwiss.Server.AutoMapper;

public class FileMappingAction(FilesProvider filesProvider) : IMappingAction<File, FileDto>
{
    public void Process(File source, FileDto destination, ResolutionContext context)
    {
        destination.Length = filesProvider.GetFileInfo(source.DataPath).Length;
    }
}