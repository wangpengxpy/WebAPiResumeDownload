using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Web;
using System.Web.Http;

namespace WebAPiResumeDownload.Controllers
{
    public class DownloadController : ApiController
    {

        private const string MimeType = "application/octet-stream";

        private const string AppSettingDirPath = "DownloadDir";

        private readonly string DirFilePath;

        public DownloadController()
        {
            this.DirFilePath = ConfigurationManager.AppSettings[AppSettingDirPath];
        }

        [HttpGet]
        public HttpResponseMessage Download(string fileName)
        {
            fileName = "HBuilder.windows.5.2.6.zip";
            HttpResponseMessage response = null;
            var fullFilePath = Path.Combine(this.DirFilePath, fileName);

            if (Request.Headers.Range == null || 
                Request.Headers.Range.Ranges.Count == 0 || 
                Request.Headers.Range.Ranges.FirstOrDefault().From.Value == 0)
            {
                var sourceStream = File.Open(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StreamContent(sourceStream);
                response.Headers.AcceptRanges.Add("bytes");
                response.Content.Headers.ContentLength = sourceStream.Length;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeType);
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = fileName
                };
            }
            else
            {
                var item = Request.Headers.Range.Ranges.FirstOrDefault();
                if (item != null && item.From.HasValue)
                {
                    response = this.GetPartialContent(fileName, item.From.Value);
                }
            }


            return response;
        }

        /// <summary>
        /// 获取下载剩余部分
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="partial"></param>
        /// <returns></returns>
        private HttpResponseMessage GetPartialContent(string fileName, long partial)
        {
            var response = new HttpResponseMessage();
            var fullFilePath = Path.Combine(this.DirFilePath, fileName);
            FileInfo fileInfo = new FileInfo(fullFilePath);
            long startByte = partial;
            Action<Stream, HttpContent, TransportContext> pushContentAction = (outputStream, content, context) =>
            {
                try
                {
                    var buffer = new byte[65536];
                    using (var fileStream = File.Open(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var bytesRead = 0;
                        fileStream.Seek(startByte, SeekOrigin.Begin);
                        int length = Convert.ToInt32((fileInfo.Length - 1) - startByte) + 1;

                        while (length > 0 && bytesRead > 0)
                        {
                            bytesRead = fileStream.Read(buffer, 0, Math.Min(length, buffer.Length));
                            outputStream.Write(buffer, 0, bytesRead);
                            length -= bytesRead;
                        }

                    }
                }
                catch (HttpException ex)
                {
                    throw ex;
                }
                finally
                {
                    outputStream.Close();
                }
            };

            response.Content = new PushStreamContent(pushContentAction, new MediaTypeHeaderValue(MimeType));
            response.StatusCode = HttpStatusCode.PartialContent;
            response.Headers.AcceptRanges.Add("bytes");
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeType);
            response.Content.Headers.ContentLength = File.Open(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read).Length;
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = fileName
            };
            return response;
        }
    }
}
