using Microsoft.AspNetCore.Mvc;

namespace ChampHeart.API.Controllers
{
    public class ResourcesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
