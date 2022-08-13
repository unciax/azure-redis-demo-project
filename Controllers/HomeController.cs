using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using redis.Models;

namespace redis.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private IDistributedCache _cache;  

        public HomeController(ILogger<HomeController> logger, IDistributedCache cache)
        {
            _logger = logger;
            _cache = cache;
        }

        public IActionResult Index()  
        {  
            //====示範儲存資料，每一次使用者進來網頁點閱數+1====  
            string clickCount = _cache.GetString("ReqeustCount") ?? "0";  
            _cache.SetString("ReqeustCount", AddOneClick(clickCount));  
            ViewData["ReqeustCount"] = clickCount;  
  
            //====示範:瀏覽使用者IP瀏覽次數，當使用者短時間瀏覽次數多送出請告，使用JSON(反)序列化集合物件====  
            var ip = GetClientUserIp(HttpContext);  
            var userIP = _cache.GetString($"userIP_{ip}") ==null ? new UserIP()  
            {  
                IP = ip,
                ReqeustCount = 0  
            } :
            Newtonsoft.Json.JsonConvert.DeserializeObject<UserIP>(_cache.GetString($"userIP_{ip}"));  
            userIP.ReqeustCount = userIP.ReqeustCount + 1;  
  
            //如果1分鐘內送出10次請求，列為黑名單  
            if(userIP.ReqeustCount > 10)  
            {  
                return Content("你已經被禁止瀏覽本網頁一分鐘");  
            }                 
            var options = new DistributedCacheEntryOptions();  
            options.SetSlidingExpiration(TimeSpan.FromMinutes(1));  
            _cache.SetString($"userIP_{ip}", Newtonsoft.Json.JsonConvert.SerializeObject(userIP), options);  
            ViewData["userIP"] = userIP.IP;  
            ViewData["userReqeustCount"] = userIP.ReqeustCount;  
            return View();  
        }  
  
  
        private string AddOneClick(string clickCount)  
        {  
            clickCount = $"{(int.Parse(clickCount) + 1)}";  
            return clickCount;  
        }  
  
        private string GetClientUserIp(HttpContext context)  
        {  
            var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();  
            if (string.IsNullOrEmpty(ip))  
            {  
                ip = context.Connection.RemoteIpAddress.ToString();  
            }  
            return ip;  
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
