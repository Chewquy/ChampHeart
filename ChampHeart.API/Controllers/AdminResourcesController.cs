using Microsoft.AspNetCore.Mvc;

namespace ChampHeart.API.Controllers
{
    public class AdminResourcesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
