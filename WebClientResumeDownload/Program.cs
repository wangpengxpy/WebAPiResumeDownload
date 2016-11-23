using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebClientResumeDownload
{
    class Program
    {
        static readonly string destinationFilePath = string.Empty;
        static void Main(string[] args)
        {
            Console.WriteLine("开始下载......");
            try
            {
                DownloadFile("http://localhost:61567/FileLocation/UML.pdf", "d:\\temp\\uml.pdf");
            }
            catch (Exception ex)
            {
                if (!string.Equals(ex.Message, "Stack Empty.", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("{0}{1}{1} 出错啦: {1} {2}", ex.Message, Environment.NewLine,
                                      ex.InnerException.ToString());
                }
            }
            Console.ReadKey();
        }

        public static void DownloadFile(string url, string filePath)
        {
            var client = new WebClient();
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) =>
            { return true; };

            try
            {
                client.OpenRead(url);

                FileInfo fileInfo = null;

                if (File.Exists(filePath))
                {
                   var finfo = new FileInfo(filePath);

                    if (client.ResponseHeaders != null &&
                        finfo.Length >= Convert.ToInt64(client.ResponseHeaders["Content-Length"]))
                    {
                        File.Delete(filePath);
                    }
                }

                DownloadFileWithResume(url, filePath);

                fileInfo = fileInfo ?? new FileInfo(destinationFilePath);

                if (fileInfo.Length == Convert.ToInt64(client.ResponseHeaders["Content-Length"]))
                {
                    Console.WriteLine("下载完成.......");
                }
                else
                {
                    throw new WebException("下载中断，请尝试重新下载......");
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine("Error: {0} {1}", ex.Message, Environment.NewLine);
                Console.WriteLine("下载中断，请尝试重新下载......");

                throw;
            }
        }


        /// <summary>
        /// 断点续传下载
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <param name="destinationPath"></param>
        private static void DownloadFileWithResume(string sourceUrl, string destinationPath)
        {
            long existLen = 0;
            FileStream saveFileStream;
            if (File.Exists(destinationPath))
            {
                var fInfo = new FileInfo(destinationPath);
                existLen = fInfo.Length;
            }
            if (existLen > 0)
                saveFileStream = new FileStream(destinationPath,
                                                            FileMode.Append, FileAccess.Write,
                                                            FileShare.ReadWrite);
            else
                saveFileStream = new FileStream(destinationPath,
                                                            FileMode.Create, FileAccess.Write,
                                                            FileShare.ReadWrite);



            var httpWebRequest = (HttpWebRequest)System.Net.HttpWebRequest.Create(sourceUrl);
            httpWebRequest.AddRange((int)existLen);
            var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var respStream = httpWebResponse.GetResponseStream())
            {
                var timout = httpWebRequest.Timeout;
                respStream.CopyTo(saveFileStream);
            }
        }
    }
}
