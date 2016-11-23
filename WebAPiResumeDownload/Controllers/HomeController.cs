using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebAPiResumeDownload.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            return View();
        }

        /// <summary>
        /// 测试下载
        /// </summary>
        /// <returns></returns>
        public ActionResult DownloadTest()
        {
            return View();
        }
    }
}
