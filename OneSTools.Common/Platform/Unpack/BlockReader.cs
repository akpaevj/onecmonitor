using System.IO.Compression;

namespace OneSTools.Common.Platform.Unpack;

public class BlockReader : Stream
{
	private BlockHeader _currentHeader;
	private readonly Stream _reader;
	private readonly int _dataSize;

	private byte[] _currentPageData;
	private int _currentPageOffset;
	private bool _isPacked;
	private bool _isContainer;

	public BlockReader(Stream basicStream)
	{
		_reader = basicStream;
		_currentHeader = BlockHeader.Read(_reader);
		_dataSize = (int)_currentHeader.DataSize;
		ReadPage();
		AnalyzeState();
	}

	private void ReadPage()
	{
		var currentDataSize = Math.Min(_dataSize, (int)_currentHeader.PageSize);
		_currentPageData = new byte[currentDataSize];
		_reader.Read(_currentPageData, 0, currentDataSize);
		_currentPageOffset = 0;
	}

	private void AnalyzeState()
	{
		var bufferToCheck = _currentPageData;
		
		try
		{
			using var inputStream = new MemoryStream(bufferToCheck);
			using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress);
			
			using var outputStream = new MemoryStream();
			deflateStream.CopyTo(outputStream);
			
			var tmp = outputStream.ToArray();
			_isPacked = true;
			bufferToCheck = tmp;
		}
		catch
		{
			_isPacked = false;
		}

		_isContainer = FileFormat.IsContainer(bufferToCheck);
	}

	private void MoveNextBlock()
	{
		if (_currentHeader.NextPageAddr == FileFormat.V8FfSignature)
		{
			_currentPageData = null;
			return;
		}
		_reader.Seek(_currentHeader.NextPageAddr, SeekOrigin.Begin);
		_currentHeader = BlockHeader.Read(_reader);
		ReadPage();
	}

	public bool IsPacked => _isPacked;

	public bool IsContainer => _isContainer;

	public override bool CanRead => true;

	public override bool CanSeek => false;

	public override bool CanWrite => false;

	public override long Length => _dataSize;

	public override long Position
	{
		get => throw new NotSupportedException();

		set => throw new NotSupportedException();
	}

	public override void Flush()
	{
		throw new NotSupportedException();
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		if (_currentPageData == null)
		{
			return 0;
		}

		var bytesRead = 0;
		var countLeft = count;

		while (countLeft > 0)
		{
			var leftInPage = _currentPageData.Length - _currentPageOffset;
			
			if (leftInPage == 0)
			{
				MoveNextBlock();
				
				if (_currentPageData == null)
				{
					break;
				}
			}

			var readFromCurrentPage = Math.Min(leftInPage, countLeft);

			Buffer.BlockCopy(_currentPageData, _currentPageOffset, buffer, offset, readFromCurrentPage);
			_currentPageOffset += readFromCurrentPage;
			offset += readFromCurrentPage;

			bytesRead += readFromCurrentPage;
			countLeft -= readFromCurrentPage;
		}

		return bytesRead;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException();
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException();
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new NotSupportedException();
	}
	
	public static byte[] ReadDataBlock(Stream reader)
	{
		var blockReader = new BlockReader(reader);
		var buf = new byte[blockReader.Length];
		blockReader.ReadExactly(buf, 0, buf.Length);
		return buf;
	}
}