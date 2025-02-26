using System.Collections;

namespace OneSTools.Common.Platform.Unpack;

public class File8Collection : IEnumerable<File8>
{
    private readonly IReadOnlyList<File8?> _data;

    public File8Collection(IEnumerable<File8?> data)
    {
        var fileList = new List<File8?>();
        fileList.AddRange(data);
        _data = fileList;
    }
    
    public int Count()
    {
        return _data.Count;
    }

    public File8? Get(int index)
    {
        return _data[index];
    }

    public File8? Get(string name)
    {
        return _data.First(f => f != null && f.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
    }
    
    public File8? Find(string name)
    {
        return _data.FirstOrDefault(f => f != null && f.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
    }

    public IEnumerator<File8> GetEnumerator()
    {
        return _data.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}