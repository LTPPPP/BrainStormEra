using BrainStormEra.Models;
using BrainStormEra.Views.Course;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra.Controllers.Course
{
    public class CourseController : Controller
    {


        private readonly SwpMainFpContext _context;
        public CourseController(SwpMainFpContext context)
        {
            _context = context;
        }



        // GET: CourseController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }




        // GET: CourseController
        public ActionResult CreateCourse()
        {
            var viewmodel = new CreateCourseViewModel
            {
                CourseCategories = _context.CourseCategories.ToList()
            };

            return View(viewmodel);

        }

        [HttpPost]
        public ActionResult CreateCourse(CreateCourseViewModel viewmodel)
        {

            if (ModelState.IsValid)
            {

                var existingCourse = _context.Courses.FirstOrDefault(c => c.CourseName == viewmodel.CourseName);
                if (existingCourse != null)
                {
                    ModelState.AddModelError("CourseName", "The Course Name already exists. Please enter a different name.");
                    viewmodel.CourseCategories = _context.CourseCategories.ToList();
                }
                var obj = new Models.Course
                {
                    CourseId = viewmodel.CourseId,
                    CourseName = viewmodel.CourseName,
                    CourseDescription = viewmodel.CourseDescription,
                    CourseStatus = 0,
                    CreatedBy = Request.Cookies["user_id"],
                    CoursePicture = viewmodel.CoursePicture.FileName,
                    Price = viewmodel.Price,
                };
                var selectedCategory = _context.CourseCategories.FirstOrDefault(c => c.CourseCategoryId == viewmodel.CourseCategoryId);

                if (selectedCategory != null)
                {
                    // Gán danh mục cho khóa học
                    obj.CourseCategories = new List<CourseCategory> { selectedCategory };
                }

                _context.Courses.Add(obj);
                _context.SaveChanges();

            }
            else
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"Error in {state.Key}: {error.ErrorMessage}");
                    }
                }
                viewmodel.CourseCategories = _context.CourseCategories.ToList();
                return View(viewmodel);
            }
            viewmodel.CourseCategories = _context.CourseCategories.ToList();
            return RedirectToAction("DeleteCourse");

        }



        [HttpGet]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCourse(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: CourseController/Edit/5
        public ActionResult EditCourse(String id)
        {
            Response.Cookies.Append("CourseId", id.ToString());

            var course = _context.Courses
                    .Include(c => c.CourseCategories)
                    .FirstOrDefault(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }
            var viewModel = new EditCourseViewModel
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                CourseCategories = _context.CourseCategories.ToList(),
                CourseCategoryId = course.CourseCategories.FirstOrDefault()?.CourseCategoryId, // Chọn Category hiện tại nếu có
                CourseDescription = course.CourseDescription,
                CoursePictureFile = course.CoursePicture,
                Price = course.Price
            };
            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCourse(EditCourseViewModel viewmodel)
        {
            if (ModelState.IsValid)
            {
                var course = _context.Courses
                    .Include(c => c.CourseCategories)
                    .FirstOrDefault(c => c.CourseId == viewmodel.CourseId);

                if (course == null)
                {
                    return NotFound();
                }
                var existingCourse = _context.Courses.FirstOrDefault(c => c.CourseName == viewmodel.CourseName);
                if (existingCourse != null)
                {
                    ModelState.AddModelError("CourseName", "The Course Name already exists. Please enter a different name.");
                    viewmodel.CourseCategories = _context.CourseCategories.ToList();
                }
                course.CourseName = viewmodel.CourseName;
                course.CourseDescription = viewmodel.CourseDescription;
                course.Price = viewmodel.Price;


                // Kiểm tra xem người dùng có chọn thay đổi ảnh không
                if (viewmodel.CoursePicture != null && viewmodel.CoursePicture.Length > 0)
                {
                    // Người dùng chọn ảnh mới, cập nhật đường dẫn ảnh
                    course.CoursePicture = Path.GetFileName(viewmodel.CoursePicture.FileName);
                }
                else
                {
                    // Nếu người dùng không chọn ảnh mới, giữ lại ảnh hiện tại
                    course.CoursePicture = course.CoursePicture;
                }
                course.CourseCategories.Clear();
                var selectedCategory = _context.CourseCategories.FirstOrDefault(c => c.CourseCategoryId == viewmodel.CourseCategoryId);
                if (selectedCategory != null)
                {
                    course.CourseCategories.Add(selectedCategory);
                }
                _context.SaveChanges();
                return RedirectToAction("DeleteCourse");
            }

            viewmodel.CourseCategories = _context.CourseCategories.ToList();
            return View(viewmodel);
        }

        // GET: CourseController/Delete/5
        public ActionResult CourseManagement()
        {
            var courses = _context.Courses
       .Include(c => c.CourseCategories)
       .Include(c => c.Enrollments)
       /*.Include(c => c.CreatedBy) */// Adjust this line
       .OrderByDescending(c => c.CourseCreatedAt)
       .Select(course => new DeleteCourseViewModel
       {
           CourseId = course.CourseId,
           CourseName = course.CourseName,
           CourseDescription = course.CourseDescription,
           CourseStatus = course.CourseStatus,
           CoursePicture = course.CoursePicture,
           Price = course.Price,
           CourseCreatedAt = course.CourseCreatedAt,
           CreatedBy = course.CreatedBy, // Adjust this line
           CourseCategories = course.CourseCategories.ToList(),
           StarRating = (byte?)Math.Round(
               _context.Feedbacks
               .Where(f => f.CourseId == course.CourseId)
               .Average(f => (double?)f.StarRating) ?? 0)
        })
       .ToList();

            return View(courses);
        }

        [HttpGet]
        public ActionResult DeleteCourse(string id)
        {
            var course = _context.Courses.Find(id);
            if (course == null)
            {
                return HttpNotFound();
            }
            _context.Courses.Remove(course);
            _context.SaveChanges();

            return RedirectToAction("DeleteCourse");
        }


        private ActionResult HttpNotFound()
        {
            throw new NotImplementedException();
        }

        // POST: CourseController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCourse(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
