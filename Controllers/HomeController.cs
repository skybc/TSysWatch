using Microsoft.AspNetCore.Mvc;

namespace TSysWatch.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Message = "欢迎访问 TSysWatch MVC 首页！";
            return View();
        }
    }
}
