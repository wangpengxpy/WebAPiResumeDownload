using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using WebAPiResumeDownload.Files;
using CustomFileInfo = WebAPiResumeDownload.Files;

namespace WebAPiResumeDownload.Controllers
{
    public class MemoryMappedFileDownloadController : ApiController
    {
        public IFileProvider FileProvider { get; set; }

        public MemoryMappedFileDownloadController()
        {
            FileProvider = new FileProvider();
        }

        private CustomFileInfo.FileInfo GetFileInfoFromRequest(HttpRequestMessage request, long entityLength)
        {
            var response = new CustomFileInfo.FileInfo
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
                var range = rangeHeader.Ranges.First();
                if (range.From.HasValue && range.From < 0 || range.To.HasValue && range.To > entityLength - 1)
                {
                    throw new HttpResponseException(HttpStatusCode.RequestedRangeNotSatisfiable);
                }

                response.From = range.From ?? 0;
                response.To = range.To ?? entityLength - 1;
                response.IsPartial = true;
                response.Length = entityLength;
                if (range.From.HasValue && range.To.HasValue)
                {
                    response.Length = range.To.Value - range.From.Value + 1;
                }
                else if (range.From.HasValue)
                {
                    response.Length = entityLength - range.From.Value + 1;
                }
                else if (range.To.HasValue)
                {
                    response.Length = range.To.Value + 1;
                }
            }

            return response;
        }

        /// <summary>
        /// 设置响应头信息
        /// </summary>
        /// <param name="response"></param>
        /// <param name="fileInfo"></param>
        /// <param name="fileLength"></param>
        /// <param name="fileName"></param>
        private void SetResponseHeaders(HttpResponseMessage response, CustomFileInfo.FileInfo fileInfo,
            long fileLength, string fileName)
        {
            response.Headers.AcceptRanges.Add("bytes");
            response.StatusCode = fileInfo.IsPartial ? HttpStatusCode.PartialContent
                : HttpStatusCode.OK;
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            response.Content.Headers.ContentDisposition.FileName = fileName;
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            response.Content.Headers.ContentLength = fileInfo.Length;
            if (fileInfo.IsPartial)
            {
                response.Content.Headers.ContentRange
                    = new ContentRangeHeaderValue(fileInfo.From, fileInfo.To, fileLength);
            }
        }


        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage GetFile(string fileName)
        {
            if (!FileProvider.Exists(fileName))
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
            long fileLength = FileProvider.GetLength(fileName);
            var fileInfo = GetFileInfoFromRequest(this.Request, fileLength);
            var mapName = string.Format("FileDownloadMap_{0}", fileName);
            MemoryMappedFile mmf = null;
            try
            {
                mmf = MemoryMappedFile.OpenExisting(mapName, MemoryMappedFileRights.Read);
            }
            catch (FileNotFoundException)
            {

                mmf = MemoryMappedFile.CreateFromFile(FileProvider.Open(fileName), mapName, fileLength,
                                                      MemoryMappedFileAccess.Read, null, HandleInheritability.None,
                                            false);
            }
            using (mmf)
            {
                Stream stream
                    = fileInfo.IsPartial
                    ? mmf.CreateViewStream(fileInfo.From, fileInfo.Length, MemoryMappedFileAccess.Read)
                    : mmf.CreateViewStream(0, fileLength, MemoryMappedFileAccess.Read);

                var response = new HttpResponseMessage();
                response.Content = new StreamContent(stream);
                SetResponseHeaders(response, fileInfo, fileLength, fileName);
                return response;
            }
        }

    }
}
