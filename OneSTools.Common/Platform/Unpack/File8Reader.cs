using System.IO.Compression;
using System.Reflection.Metadata;

namespace OneSTools.Common.Platform.Unpack;

public class File8Reader : IDisposable
{
	private readonly Stream _reader;
	private readonly bool _dataPacked;
	private int _storageVersion;
	
	public decimal StorageVersion => _storageVersion;
	public File8Collection Elements { get; }

	public File8Reader(string filePath, bool dataPacked = true)
	{
		const int magicSize = 100 * 1024;
		var fileStream = new FileStream(filePath, FileMode.Open);
		if (fileStream.Length >= magicSize)
			_reader = fileStream;
		else
		{
			var memoryStream = new MemoryStream();
			fileStream.CopyTo(memoryStream);
			memoryStream.Seek(0, SeekOrigin.Begin);
			
			_reader = memoryStream;
		}
		
		_dataPacked = dataPacked;
		var fileList = ReadFileList();
		Elements = new File8Collection(fileList);
	}

	public File8Reader(Stream stream, bool dataPacked = true)
	{
		_reader = stream;
		_dataPacked = dataPacked;
		var fileList = ReadFileList();
		Elements = new File8Collection(fileList);
	}

	private List<File8> ReadFileList()
	{
		var containerHeader = ContainerHeader.Read(_reader);
		_storageVersion = (int)containerHeader.StorageVer;
		var elemsAddrBuf = BlockReader.ReadDataBlock(_reader);
		var addresses = ElementAddress.Parse(elemsAddrBuf);

		var fileList = new List<File8>();
		foreach (var address in addresses)
		{
			if (address.HeaderAddress == FileFormat.V8FfSignature || address.Signature != FileFormat.V8FfSignature)
				continue;

			_reader.Seek(address.HeaderAddress, SeekOrigin.Begin);
			var buf = BlockReader.ReadDataBlock(_reader);

			var fileHeader = ElementHeader.Parse(buf);
			fileList.Add(new File8(fileHeader, address.DataAddress));
		}

		return fileList;
	}
	
	public void Extract(File8 element, string destDir, bool recursiveUnpack = false)
	{

		if (!Directory.Exists(destDir))
		{
			Directory.CreateDirectory(destDir);
		}

		Stream fileExtractor;

		if (element.DataOffset == FileFormat.V8FfSignature)
		{
			// Файл есть, но пуст
			fileExtractor = new MemoryStream();
		}
		else
		{
			_reader.Seek(element.DataOffset, SeekOrigin.Begin);

			var blockExtractor = new BlockReader(_reader);
			if (blockExtractor.IsPacked && _dataPacked)
			{
				fileExtractor = new DeflateStream(blockExtractor, CompressionMode.Decompress);
			}
			else
			{
				fileExtractor = blockExtractor;
			}

			if (blockExtractor.IsContainer && recursiveUnpack)
			{
				var outputDirectory = Path.Combine(destDir, element.Name);
				var tmpData = new MemoryStream(); // TODO: переделать MemoryStream --> FileStream
				fileExtractor.CopyTo(tmpData);
				tmpData.Seek(0, SeekOrigin.Begin);

				var internalContainer = new File8Reader(tmpData, dataPacked: false);
				internalContainer.ExtractAll(outputDirectory, recursiveUnpack);

				return;
			}
		}

		// Просто файл
		var outputFileName = Path.Combine(destDir, element.Name);
		using var outputFile = new FileStream(outputFileName, FileMode.Create);
		fileExtractor.CopyTo(outputFile);
	}

	/// <summary>
	/// Извлекает все файлы из контейнера.
	/// </summary>
	/// <param name="destDir">Каталог назначения.</param>
	/// <param name="recursiveUnpack">Если установлен в Истина, то все найденные вложенные восьмофайлы
	/// будут распакованы в отдельные подкаталоги. Необязательный.</param>
	public void ExtractAll(string destDir, bool recursiveUnpack = false)
	{
		foreach (var element in Elements)
		{
			Extract(element, destDir, recursiveUnpack);
		}
	}

	public void Dispose()
	{
		_reader.Close();
	}
	
	public void Close()
	{
		_reader.Close();
	}
}