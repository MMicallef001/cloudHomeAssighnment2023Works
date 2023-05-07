using Microsoft.AspNetCore.Mvc;

namespace TranscribeService.Controllers
{
    public class PubSubFunctionController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
