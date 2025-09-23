using System.Reflection;

namespace OneSwiss.V8.Platform.Brackets
{
    internal static class StreamReaderExtensions
    {
        private static readonly FieldInfo CharPosField = typeof(StreamReader).GetField("_charPos",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        private static readonly FieldInfo ByteLenField = typeof(StreamReader).GetField("_byteLen",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        private static readonly FieldInfo CharBufferField = typeof(StreamReader).GetField("_charBuffer",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        public static long GetPosition(this StreamReader reader)
        {
            // shift position back from BaseStream.Position by the number of bytes read
            // into internal buffer.
            var byteLen = (int)ByteLenField.GetValue(reader);
            var position = reader.BaseStream.Position - byteLen;

            // if we have consumed chars from the buffer we need to calculate how many
            // bytes they represent in the current encoding and add that to the position.
            var charPos = (int)CharPosField.GetValue(reader);

            if (charPos <= 0)
                return position;

            var charBuffer = (char[])CharBufferField.GetValue(reader);
            var encoding = reader.CurrentEncoding;
            var bytesConsumed = encoding.GetBytes(charBuffer, 0, charPos).Length;
            position += bytesConsumed;

            return position;
        }

        public static void SetPosition(this StreamReader reader, long position)
        {
            reader.DiscardBufferedData();
            reader.BaseStream.Seek(position, SeekOrigin.Begin);

            if (reader.BaseStream.Position != position)
                throw new Exception("Couldn't set the stream position");
        }
    }
}