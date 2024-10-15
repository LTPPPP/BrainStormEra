using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult HomePageAdmin()
    {
        return View();
    }
    [HttpGet]
    public IActionResult HomePageInstructor()
    {
        return View();
    }
    [HttpGet]
    public IActionResult HomePageLearner()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ChapterManagement()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ViewChapters()
    {
        return View();
    }
    [HttpGet]
    public IActionResult DeleteChapter()
    {
        return View();
    }
    [HttpGet]
    public IActionResult EditChapter()
    {
        return View();
    }
    [HttpGet]
    public IActionResult LessonManagement()
    {
        return View();
    }
}
