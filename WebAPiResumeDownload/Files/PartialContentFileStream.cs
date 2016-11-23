using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace WebAPiResumeDownload.Files
{
    public class PartialContentFileStream : Stream
    {
        private readonly long _start;
        private readonly long _end;
        private long _position;
        private FileStream _fileStream;
        public PartialContentFileStream(FileStream fileStream, long start, long end)
        {
            _start = start;
            _position = start;
            _end = end;
            _fileStream = fileStream;

            if (start > 0)
            {
                _fileStream.Seek(start, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// 将缓冲区数据写到文件
        /// </summary>
        public override void Flush()
        {
            _fileStream.Flush();
        }

        /// <summary>
        /// 设置当前下载位置
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                _position = _start + offset;
                return _fileStream.Seek(_start + offset, origin);
            }
            else if (origin == SeekOrigin.Current)
            {
                _position += offset;
                return _fileStream.Seek(_position + offset, origin);
            }
            else
            {
                throw new NotImplementedException("SeekOrigin.End未实现");
            }
        }

        /// <summary>
        /// 依据偏离位置读取
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int byteCountToRead = count;
            if (_position + count > _end)
            {
                byteCountToRead = (int)(_end - _position) + 1;
            }
            var result = _fileStream.Read(buffer, offset, byteCountToRead);
            _position += byteCountToRead;
            return result;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            int byteCountToRead = count;
            if (_position + count > _end)
            {
                byteCountToRead = (int)(_end - _position);
            }
            var result = _fileStream.BeginRead(buffer, offset,
                                               count, (s) =>
                                               {
                                                   _position += byteCountToRead;
                                                   callback(s);
                                               }, state);
            return result;
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _fileStream.EndRead(asyncResult);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        public override int ReadByte()
        {
            int result = _fileStream.ReadByte();
            _position++;
            return result;
        }

        public override void WriteByte(byte value)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _end - _start; }
        }

        public override long Position
        {
            get { return _position; }
            set
            {
                _position = value;
                _fileStream.Seek(_position, SeekOrigin.Begin);
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fileStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}