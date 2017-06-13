using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WordPressPluginAnalytics.Lib
{
    public class ExtractionStream : Stream
    {
        private bool _end = false;
        private int _position;
        private IEnumerator<byte> _data;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Position { get => _position; set => throw new NotImplementedException(); }

        // no need to implement write-related interface
        public override long Length => throw new NotImplementedException();
        public override void Flush() => throw new NotImplementedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        public ExtractionStream(IEnumerable<string> lines)
        {
            _data = GetBytes(lines).GetEnumerator();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int pos = offset;
            for(int i = 0; !_end && i < count && _data.MoveNext(); i++)
            {
                buffer[pos++] = _data.Current;
            }

            var dataRead = pos - offset;
            _position += dataRead;
            return dataRead;
        }

        private IEnumerable<byte> GetBytes(IEnumerable<string> lines)
        {
            foreach(var line in lines)
            {
                var buffer = Encoding.UTF8.GetBytes(line);
                foreach(var b in buffer)
                {
                    yield return b;
                }
            }
        }
    }
}
