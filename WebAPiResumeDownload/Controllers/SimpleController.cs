using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;

namespace WebAPiResumeDownload.Controllers
{
    public class SimpleController : ApiController
    {
        private const int BufferSize = 80 * 1024;

        private const string MimeType = "application/octet-stream";

        private const string AppSettingDirPath = "DownloadDir";

        private readonly string DirFilePath;
        public SimpleController()
        {
            this.DirFilePath = ConfigurationManager.AppSettings[AppSettingDirPath];
            this.DirFilePath = HostingEnvironment.MapPath(DirFilePath);
        }

        [HttpGet]
        public HttpResponseMessage Download(string fileName)
        {
            var fullFilePath = Path.Combine(this.DirFilePath, fileName);
            if (!File.Exists(fullFilePath))
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
            FileStream fileStream = File.Open(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var response = new HttpResponseMessage();
            response.Content = new StreamContent(fileStream, BufferSize);
            response.Content.Headers.ContentDisposition
                = new ContentDispositionHeaderValue("attachment") { FileName = fileName };
            response.Content.Headers.ContentType
                = new MediaTypeHeaderValue(MimeType);
            response.Content.Headers.ContentLength
                = fileStream.Length;
            return response;
        }

    }
}
