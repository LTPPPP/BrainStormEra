using BrainStormEra.Models;
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Index", "HomePageGuest");
    }

    public IActionResult Error()
    {
        var errorModel = new ErrorViewModel { /* set properties */ };
        return View(errorModel);
    }

    [HttpGet]
    public IActionResult Privacy()
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
    public IActionResult DeleteChapter(int id)
    {
        return View();
    }

    [HttpGet]
    public IActionResult EditChapter(int id)
    {
        return View();
    }

    [HttpGet]
    public IActionResult CourseDetail(int id)
    {
        return View();
    }

    [HttpGet]
    public IActionResult CreateCourse()
    {
        return View();
    }

    [HttpGet]
    public IActionResult DeleteCourse(int id)
    {
        return View();
    }

    [HttpGet]
    public IActionResult EditCourse(int id)
    {
        return View();
    }

    [HttpGet]
    public IActionResult ReviewCourse(int id)
    {
        return View();
    }

    [HttpGet]
    public IActionResult SearchCourse(string searchTerm)
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
    public IActionResult DeleteLesson(int id)
    {
        return View();
    }

    [HttpGet]
    public IActionResult EditLesson(int id)
    {
        return View();
    }

    [HttpGet]
    public IActionResult Notifications()
    {
        return View();
    }

    [HttpGet]
    public IActionResult CreateNotification()
    {
        return View();
    }

    [HttpGet]
    public IActionResult EditNotification(int id)
    {
        return View();
    }

    [HttpGet]
    public IActionResult DeleteNotification(int id)
    {
        return View();
    }

    [HttpGet]
    public IActionResult Certificates(int id)
    {
        return View();
    }
    [HttpGet]
    public IActionResult ViewCertificate(int id)
    {
        return View();
    }


    [HttpGet]
    public IActionResult LoginPage()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Chat()
    {
        return View();
    }

    [HttpGet]
    public IActionResult _NotificationModal()
    {
        return View();
    }

    [HttpGet]
    public IActionResult addNotificationModal()
    {
        return View();
    }

    [HttpGet]
    public IActionResult editNotificationModal()
    {
        return View();
    }
}
