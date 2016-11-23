using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;

namespace WebAPiResumeDownload.Files
{
    public class FileProvider : IFileProvider
    {
        private readonly string _filesDirectory;
        private const string AppSettingsKey = "DownloadDir";

        public FileProvider()
        {
            var fileLocation = ConfigurationManager.AppSettings[AppSettingsKey];
            if (!String.IsNullOrWhiteSpace(fileLocation))
            {
                _filesDirectory = HostingEnvironment.MapPath(fileLocation);
            }
        }

        /// <summary>
        /// 判断文件是否存在
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Exists(string name)
        {
            string file = Directory.GetFiles(_filesDirectory, name, SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();
            return true;
        }


        /// <summary>
        /// 打开文件
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FileStream Open(string name)
        {
            var fullFilePath = Path.Combine(_filesDirectory, name);
            return File.Open(fullFilePath,
                FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        /// <summary>
        /// 获取文件长度
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public long GetLength(string name)
        {
            var fullFilePath = Path.Combine(_filesDirectory, name);
            return new System.IO.FileInfo(fullFilePath).Length;
        }
    }
}