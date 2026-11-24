using Microsoft.AspNetCore.Mvc;

namespace ChampHeart.API.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
