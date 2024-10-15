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
    public IActionResult CourseDetail()
    {
        return View();
    }
    [HttpGet]
    public IActionResult EditCourse()
    {
        return View();
    }
    [HttpGet]
    public IActionResult ReviewCourse()
    {
        return View();
    }
    [HttpGet]
    public IActionResult SearchCourse()
    {
        return View();
    }
    [HttpGet]
    public IActionResult Analysis()
    {
        return View();
    }

    [HttpGet]
    public IActionResult LessonManagement()
    {
        return View();
    }
    [HttpGet]
    public IActionResult CreateLesson()
    {
        return View();
    }
    [HttpGet]
    public IActionResult CreateNotification()
    {
        return View();
    }
    [HttpGet]
    public IActionResult Notification()
    {
        return View();
    }
    [HttpGet]
    public IActionResult EditNotification()
    {
        return View();
    }
}
