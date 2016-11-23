using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using WebAPiResumeDownload.Files;

namespace WebAPiResumeDownload.Controllers
{
    public class FileDownloadController : ApiController
    {
        private const int BufferSize = 80 * 1024;
        private const string MimeType = "application/octet-stream";
        public IFileProvider FileProvider { get; set; }
        public FileDownloadController()
        {
            FileProvider = new FileProvider();
        }


        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public HttpResponseMessage GetFile(string fileName)
        {
            if (!FileProvider.Exists(fileName))
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
            long fileLength = FileProvider.GetLength(fileName);
            var fileInfo = GetFileInfoFromRequest(this.Request, fileLength);

            //获取剩余部分文件流
            var stream = new PartialContentFileStream(FileProvider.Open(fileName),
                                                 fileInfo.From, fileInfo.To);
            var response = new HttpResponseMessage();
            response.Content = new StreamContent(stream, BufferSize);
            SetResponseHeaders(response, fileInfo, fileLength, fileName);
            return response;
        }


        /// <summary>
        /// 根据请求信息赋予封装的文件信息类
        /// </summary>
        /// <param name="request"></param>
        /// <param name="entityLength"></param>
        /// <returns></returns>
        private FileInfo GetFileInfoFromRequest(HttpRequestMessage request, long entityLength)
        {
            var fileInfo = new FileInfo
            {
                From = 0,
                To = entityLength - 1,
                IsPartial = false,
                Length = entityLength
            };
            var rangeHeader = request.Headers.Range;
            if (rangeHeader != null && rangeHeader.Ranges.Count != 0)
            {
                if (rangeHeader.Ranges.Count > 1)
                {
                    throw new HttpResponseException(HttpStatusCode.RequestedRangeNotSatisfiable);
                }
                RangeItemHeaderValue range = rangeHeader.Ranges.First();
                if (range.From.HasValue && range.From < 0 || range.To.HasValue && range.To > entityLength - 1)
                {
                    throw new HttpResponseException(HttpStatusCode.RequestedRangeNotSatisfiable);
                }

                fileInfo.From = range.From ?? 0;
                fileInfo.To = range.To ?? entityLength - 1;
                fileInfo.IsPartial = true;
                fileInfo.Length = entityLength;
                if (range.From.HasValue && range.To.HasValue)
                {
                    fileInfo.Length = range.To.Value - range.From.Value + 1;
                }
                else if (range.From.HasValue)
                {
                    fileInfo.Length = entityLength - range.From.Value + 1;
                }
                else if (range.To.HasValue)
                {
                    fileInfo.Length = range.To.Value + 1;
                }
            }

            return fileInfo;
        }

        /// <summary>
        /// 设置响应头信息
        /// </summary>
        /// <param name="response"></param>
        /// <param name="fileInfo"></param>
        /// <param name="fileLength"></param>
        /// <param name="fileName"></param>
        private void SetResponseHeaders(HttpResponseMessage response, FileInfo fileInfo,
                                      long fileLength, string fileName)
        {
            response.Headers.AcceptRanges.Add("bytes");
            response.StatusCode = fileInfo.IsPartial ? HttpStatusCode.PartialContent
                                      : HttpStatusCode.OK;
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            response.Content.Headers.ContentDisposition.FileName = fileName;
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeType);
            response.Content.Headers.ContentLength = fileInfo.Length;
            if (fileInfo.IsPartial)
            {
                response.Content.Headers.ContentRange
                    = new ContentRangeHeaderValue(fileInfo.From, fileInfo.To, fileLength);
            }
        }
    }
}
