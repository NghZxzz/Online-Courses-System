using DoAnChuyennNganh.Data;
using DoAnChuyennNganh.Models;
using DoAnChuyennNganh.Models.Vnpay;
using DoAnChuyennNganh.Services;
using DoAnChuyennNganh.Services.VnPay;
using Google.Apis.Drive.v3.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnChuyennNganh.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly GoogleDriveService _googleDriveService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IVnPayService _vnPayService;


        public StudentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IVnPayService vnPayService, GoogleDriveService googleDriveService)
        {
            _context = context;
            _userManager = userManager;
            _vnPayService = vnPayService;
            _googleDriveService = googleDriveService;
        }
        public async Task<IActionResult> ListCourses(string searchQuery, string subjectFilter, string teacherFilter, int? minPrice, int? maxPrice, int pageNumber = 1)
        {
            int pageSize = 3;
            var userId = _userManager.GetUserId(User);

            var purchasedCourseIds = await _context.vnPays
                .Where(v => v.UserId == userId && v.TransactionId != null)
                .Select(v => v.CourseId)
                .ToListAsync();

            var coursesQuery = _context.Courses
                .Include(c => c.Sections)
                .ThenInclude(s => s.Lectures)
                .Include(c => c.User)
                .Include(c => c.Subject)
                .AsQueryable();

            // Lọc các điều kiện tìm kiếm
            if (!string.IsNullOrEmpty(searchQuery))
            {
                coursesQuery = coursesQuery.Where(c => c.Name.Contains(searchQuery));
            }

            if (!string.IsNullOrEmpty(subjectFilter))
            {
                coursesQuery = coursesQuery.Where(c => c.Subject.Name == subjectFilter);
            }

            if (!string.IsNullOrEmpty(teacherFilter))
            {
                coursesQuery = coursesQuery.Where(c => (c.User.FirstName + " " + c.User.LastName).Contains(teacherFilter));
            }

            if (minPrice.HasValue)
            {
                coursesQuery = coursesQuery.Where(c => c.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                coursesQuery = coursesQuery.Where(c => c.Price <= maxPrice.Value);
            }

            var courses = coursesQuery
                .AsEnumerable()
                .Where(c => !purchasedCourseIds.Contains(c.Id) && c.Status == CourseStatus.Approved)
                .ToList();

            var courseRatings = await _context.Reviews
                .GroupBy(r => r.CourseId)
                .Select(g => new
                {
                    CourseId = g.Key,
                    AverageRating = g.Average(r => (double?)r.Rating) ?? 5
                })
                .ToListAsync();

            var courseList = courses.Select(c => new
            {
                Course = c,
                LectureCount = c.Sections.Sum(s => s.Lectures.Count),
                AverageRating = courseRatings.FirstOrDefault(cr => cr.CourseId == c.Id)?.AverageRating ?? 5,
                TeacherName = c.User != null ? c.User.FirstName + " " + c.User.LastName : "N/A",
                SubjectName = c.Subject != null ? c.Subject.Name : "N/A"
            }).ToList();

            int totalCourses = courseList.Count;
            var paginatedCourses = courseList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Subjects = await _context.Subjects.ToListAsync();

            // Lưu giá trị tìm kiếm vào ViewBag
            ViewBag.SearchQuery = searchQuery;
            ViewBag.SubjectFilter = subjectFilter;
            ViewBag.TeacherFilter = teacherFilter;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCourses / pageSize);

            return View(paginatedCourses);
        }


        [Authorize]
        public async Task<IActionResult> ListCoursesUser()
        {
            var userId = _userManager.GetUserId(User);

            // Lấy danh sách các CourseId mà người dùng đã mua
            var purchasedCourseIds = await _context.vnPays
                .Where(v => v.UserId == userId && v.TransactionId != null)
                .Select(v => v.CourseId)
                .ToListAsync();

            // Lấy danh sách các bài giảng đã hoàn thành của người dùng
            var completedLectureIds = await _context.CompletedLectures
                .Where(cl => cl.UserId == userId)
                .Select(cl => cl.LectureId)
                .ToListAsync();

            // Lấy danh sách các khóa học tương ứng
            var courses = _context.Courses
                .Include(c => c.Sections)
                .ThenInclude(s => s.Lectures)
                .AsEnumerable()
                .Where(c => purchasedCourseIds.Contains(c.Id))
                .ToList();

            var courseRatings = await _context.Reviews
                .GroupBy(r => r.CourseId)
                .Select(g => new
                {
                    CourseId = g.Key,
                    AverageRating = g.Average(r => (double?)r.Rating) ?? 5 // Nếu chưa có đánh giá, mặc định là 5
                })
                .ToListAsync();

            // Chuẩn bị dữ liệu để truyền vào View
            var courseList = courses.Select(c => new
            {
                Course = c,
                LectureCount = c.Sections.Sum(s => s.Lectures.Count),
                CompletedLectureCount = c.Sections.Sum(s => s.Lectures.Count(l => completedLectureIds.Contains(l.Id))),
                AverageRating = courseRatings.FirstOrDefault(cr => cr.CourseId == c.Id)?.AverageRating ?? 5
            }).ToList();

            return View(courseList);
        }


        public async Task<IActionResult> DetailsCourses(int id)
        {
            var course = await _context.Courses
            .Include(c => c.Sections)
            .ThenInclude(s => s.Lectures)
            .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }
            var reviews = await _context.Reviews
            .Include(r => r.User)
            .Where(r => r.CourseId == id)
            .ToListAsync();

            ViewBag.Reviews = reviews;
            
            // Bước 2: Tạo danh sách LectureId từ các bài giảng trong khóa học
            var lectureIdsInCourse = course.Sections
                .SelectMany(s => s.Lectures)
                .AsEnumerable()
                .Select(l => l.Id)
                .ToList();

            // Bước 3: Lọc các bài giảng đã hoàn thành của người dùng
            var userId = _userManager.GetUserId(User);
            var completedLectureIds = _context.CompletedLectures
            .AsEnumerable() // Chuyển sang client-side evaluation
            .Where(cl => cl.UserId == userId && lectureIdsInCourse.Contains(cl.LectureId))
            .Select(cl => cl.LectureId)
            .ToList();

            // Bước 4: Tính toán tiến độ hoàn thành và truyền vào View
            var completedLecturesCount = completedLectureIds.Count;
            var totalLecturesCount = lectureIdsInCourse.Count;
            var completionPercentage = totalLecturesCount > 0 ? (completedLecturesCount * 100) / totalLecturesCount : 0;

            ViewBag.UserId = userId;
            ViewData["CompletionPercentage"] = completionPercentage;
            ViewBag.CompletedLectureIds = completedLectureIds;
            return View(course);
        }
        [HttpPost]
        public async Task<IActionResult> SubmitReview(Review model)
        {
            var userId = _userManager.GetUserId(User);

            // Kiểm tra nếu người dùng đã đánh giá khóa học
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.CourseId == model.CourseId);

            if (existingReview != null)
            {
                TempData["ErrorMessage"] = "Bạn đã đánh giá khóa học này.";
                return RedirectToAction("DetailsCourses", new { id = model.CourseId });
            }

            // Gắn UserId vào model
            model.CreatedAt = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi gửi đánh giá. Vui lòng kiểm tra lại.";
                return RedirectToAction("DetailsCourses", new { id = model.CourseId });
            }

            _context.Reviews.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đánh giá của bạn đã được gửi thành công!";
            return RedirectToAction("DetailsCourses", new { id = model.CourseId });
        }
        public async Task<IActionResult> DetailsLecture(int id, int courseId)
        {
            var lecture = await _context.Lectures
                .Include(l => l.Quiz)
                    .ThenInclude(q => q.Questions)
                        .ThenInclude(question => question.Answers)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lecture == null)
            {
                return NotFound();
            }
            var userId = _userManager.GetUserId(User);
            var isCompleted = await _context.CompletedLectures
                .AnyAsync(cl => cl.UserId == userId && cl.LectureId == id);
            ViewBag.IsCompleted = isCompleted;
            ViewBag.CourseId = courseId;
            var quizResults = lecture.Quiz?.Questions?.Select(q => new QuestionResult
            {
                QuestionId = q.Id,
                QuestionText = q.Text,
                AnswerOptions = q.Answers.Select(a => new AnswerOption
                {
                    AnswerId = a.Id,
                    AnswerText = a.Text,
                    IsCorrect = a.IsCorrect
                }).ToList()
            }).ToList() ?? new List<QuestionResult>(); // Trả về danh sách trống nếu không có câu hỏi

            var quizModel = new QuizSubmissionModel
            {
                LectureId = lecture.Id,
                LectureName = lecture.Name,
                Video_url = lecture.Video_url,
                Document_url = lecture.Document_url,
                Description = lecture.Description,
                QuizResults = quizResults,
                HasSubmitted = false
            };

            return View(quizModel);
        }

        [HttpPost]
        public IActionResult SubmitQuiz(QuizSubmissionRequest request)
        {
            if (request == null || request.Answers == null || !request.Answers.Any())
            {
                return BadRequest("Không có đáp án nào được gửi lên.");
            }

            var lecture = _context.Lectures
                .Include(l => l.Quiz)
                    .ThenInclude(q => q.Questions)
                        .ThenInclude(question => question.Answers)
                .FirstOrDefault(l => l.Id == request.LectureId);

            if (lecture == null)
            {
                return NotFound();
            }
            var totalQuestions = lecture.Quiz.Questions.Count;
            var correctAnswers = 0;

            // Xử lý kết quả trắc nghiệm
            var quizResults = lecture.Quiz.Questions.Select(question => new QuestionResult
            {
                QuestionId = question.Id,
                QuestionText = question.Text,
                SelectedAnswerId = request.Answers.FirstOrDefault(a => a.QuestionId == question.Id)?.SelectedAnswerId,
                AnswerOptions = question.Answers.Select(answer => new AnswerOption
                {
                    AnswerId = answer.Id,
                    AnswerText = answer.Text,
                    IsCorrect = answer.IsCorrect,
                    IsSelected = answer.Id == request.Answers.FirstOrDefault(a => a.QuestionId == question.Id)?.SelectedAnswerId
                }).ToList()
            }).ToList();
            foreach (var question in quizResults)
            {
                var selectedAnswer = question.AnswerOptions.FirstOrDefault(a => a.IsSelected);
                if (selectedAnswer != null && selectedAnswer.IsCorrect)
                {
                    correctAnswers++;
                }
            }
            var quizModel = new QuizSubmissionModel
            {
                LectureId = lecture.Id,
                LectureName = lecture.Name,
                Video_url = lecture.Video_url,
                Document_url = lecture.Document_url,
                Description = lecture.Description,
                QuizResults = quizResults,
                HasSubmitted = true
            };
            var score = (correctAnswers * 100) / totalQuestions;
            TempData["SuccessMessage"] = $"Bạn đã hoàn thành bài quiz với {score}% chính xác!";
            ViewBag.CourseId = request.CourseId;

            return View("DetailsLecture", quizModel);
        }
        [HttpPost]
        public async Task<IActionResult> MarkAsCompleted(int lectureId, int courseId)
        {
            var userId = _userManager.GetUserId(User);
            var completedLecture = await _context.CompletedLectures
                .FirstOrDefaultAsync(cl => cl.UserId == userId && cl.LectureId == lectureId);

            if (completedLecture == null)
            {
                _context.CompletedLectures.Add(new CompletedLecture
                {
                    UserId = userId,
                    LectureId = lectureId,
                    CompletedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("DetailsLecture", new { id = lectureId, courseId = courseId });
        }
        [HttpPost]
        public async Task<IActionResult> UndoCompletion(int lectureId, int courseId)
        {
            var userId = _userManager.GetUserId(User);
            var completedLecture = await _context.CompletedLectures
                .FirstOrDefaultAsync(cl => cl.UserId == userId && cl.LectureId == lectureId);

            if (completedLecture != null)
            {
                _context.CompletedLectures.Remove(completedLecture);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("DetailsLecture", new { id = lectureId, courseId = courseId });
        }
        public async Task<IActionResult> CourseInfo(int id)
        {
            var course = await _context.Courses
                .Include(c => c.User) // Thông tin giảng viên
                .Include(c => c.Sections) // Danh sách các chương
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }
            var reviews = await _context.Reviews
            .Include(r => r.User)
            .Where(r => r.CourseId == id)
            .ToListAsync();

            ViewBag.Reviews = reviews;

            return View(course);
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePaymentUrlVnpay(PaymentInformationModel model)
        {
            var userId = _userManager.GetUserId(User); // Lấy UserId từ User
            var url = await _vnPayService.CreatePaymentUrl(model, HttpContext, userId); // Gọi phương thức async
            return Redirect(url);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response.Success)
            {

                // Lưu giao dịch vào cơ sở dữ liệu
                var paymentRecord = new VnPayModel
                {
                    OrderId = response.OrderId,
                    OrderDescription = response.OrderDescription,
                    UserId = _userManager.GetUserId(User),
                    Price = response.Price,
                    CourseId = response.CourseId, // Sử dụng CourseId đã tách
                    PaymentMethod = response.PaymentMethod,
                    PaymentId = response.PaymentId,
                    TransactionId = response.TransactionId,
                    CreatedDate = DateTime.UtcNow
                };

                _context.vnPays.Add(paymentRecord);
                await _context.SaveChangesAsync();

                return View("PaymentSuccess", paymentRecord);
            }
            else
            {
                // Hiển thị view thất bại
                return View("PaymentFail");
            }
        }
        [HttpGet]
        public async Task<IActionResult> History()
        {
            var userId = _userManager.GetUserId(User);

            // Truy vấn các đơn hàng của người dùng
            var userOrders = await _context.vnPays
                .Where(order => order.UserId == userId)
                .ToListAsync();
            return View(userOrders);
        }

        [HttpGet]
        public async Task<IActionResult> ApplyForTeacher()
        {
            var userId = _userManager.GetUserId(User);
            ViewBag.Username = _userManager.GetUserName(User);
            ViewBag.UserId = userId;
            // Kiểm tra nếu người dùng đã có đơn đang chờ duyệt hoặc đã được duyệt
            var existingApplication = await _context.TeacherApplications
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync(a => a.UserId == userId &&
                                          (a.Status == ApplicationStatus.Pending || a.Status == ApplicationStatus.Approved));
            if (existingApplication != null)
            {
                TempData["ErrorMessage"] = "Bạn đã có đơn đăng ký đang chờ duyệt. Vui lòng chờ quyết định.";
                return RedirectToAction("MyApplication"); // Chuyển hướng đến trang hiển thị đơn
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyForTeacher(TeacherApplication model, IFormFileCollection files)
        {
            ViewBag.Username = _userManager.GetUserName(User);


            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // Xử lý upload file minh chứng
            if (files != null && files.Any())
            {
                foreach (var file in files)
                {
                    if (file.ContentType != "application/pdf")
                    {
                        ModelState.AddModelError("", "Chỉ chấp nhận file PDF.");
                        return View(model);
                    }

                    var fileUrl = await _googleDriveService.UploadFileAsync(file); // Lưu lên Google Drive
                    var applicationFile = new TeacherApplicationFile
                    {
                        FileName = file.FileName,
                        FileUrl = $"https://drive.google.com/file/d/{fileUrl}/view"
                    };
                    model.ApplicationFiles.Add(applicationFile);
                }
            }

            // Lưu vào cơ sở dữ liệu
            _context.TeacherApplications.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đơn đăng ký của bạn đã được gửi thành công!";
            return RedirectToAction("MyApplication");
        }
        [HttpGet]
        public async Task<IActionResult> MyApplication()
        {
            var userId = _userManager.GetUserId(User);

            // Lấy đơn gần nhất của người dùng
            var application = await _context.TeacherApplications
                .Include(a => a.ApplicationFiles)
                .FirstOrDefaultAsync(a => a.UserId == userId);

            return View(application);
        }

        [HttpGet]
        public IActionResult Chatbot()
        {
            return View();
        }
    }
}
