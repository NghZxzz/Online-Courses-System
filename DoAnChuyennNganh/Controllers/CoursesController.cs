using DoAnChuyennNganh.Data;
using DoAnChuyennNganh.Models;
using DoAnChuyennNganh.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using static System.Collections.Specialized.BitVector32;

namespace DoAnChuyennNganh.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly GoogleDriveService _googleDriveService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ImgurService _imgurService;

        public CoursesController(ApplicationDbContext context, GoogleDriveService googleDriveService, UserManager<ApplicationUser> userManager, ImgurService imgurService)
        {
            _context = context;
            _googleDriveService = googleDriveService;
            _userManager = userManager;
            _imgurService = imgurService;
        }
        private string ExtractFileIdFromUrl(string url)
        {
            var match = System.Text.RegularExpressions.Regex.Match(url, @"[-\w]{25,}");
            return match.Success ? match.Value : null;
        }

        public async Task<IActionResult> ListCourses()
        {
            var userId = _userManager.GetUserId(User);

            var courses = await _context.Courses
                .Where(c => c.UserId == userId) // Lọc khóa học theo giảng viên đang đăng nhập
                .Include(c => c.Sections)
                .ToListAsync();
            return View(courses);
        }
        public async Task<IActionResult> CreateCourses()
        {
            var categories = await _context.Subjects.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourses(Courses courses, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                courses.UserId = userId;
                // Kiểm tra nếu có file hình ảnh
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Upload hình ảnh lên Imgur và lấy URL
                    var imageUrl = await _imgurService.UploadImageAsync(imageFile);
                    courses.Image_url = imageUrl;
                }

                // Lưu Courses vào cơ sở dữ liệu
                _context.Courses.Add(courses);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(ListCourses)); // Hoặc chuyển đến trang khác
            }

            var categories = await _context.Subjects.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");

            return View(courses);
        }
        public async Task<IActionResult> EditCourses(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(_context.Subjects, "Id", "Name", course.SubjectId); // Danh sách chủ đề
            return View(course);
        }

        // POST: EditCourses
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourses(int id, Courses course, IFormFile? imageFile)
        {
            if (id != course.Id)
            {
                return BadRequest();
            }

            var existingCourse = await _context.Courses.FindAsync(id);
            if (existingCourse == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Cập nhật hình ảnh nếu có file mới
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Upload ảnh lên Imgur và lấy URL
                    var imageUrl = await _imgurService.UploadImageAsync(imageFile);
                    existingCourse.Image_url = imageUrl;
                }

                // Cập nhật các thông tin khác của khóa học
                existingCourse.Name = course.Name;
                existingCourse.Description = course.Description;
                existingCourse.Price = course.Price;
                existingCourse.SubjectId = course.SubjectId;

                // Lưu thay đổi vào cơ sở dữ liệu
                _context.Update(existingCourse);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(ListCourses)); // Quay lại danh sách khóa học
            }

            ViewBag.Categories = new SelectList(_context.Subjects, "Id", "Name", course.SubjectId); // Truyền lại danh sách chủ đề nếu xảy ra lỗi
            return View(course);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            // Tìm khóa học
            var course = await _context.Courses
                .Include(c => c.Sections)
                .ThenInclude(s => s.Lectures)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                TempData["ErrorMessage"] = "Khóa học không tồn tại.";
                return RedirectToAction("ListCourses");
            }

            // Xóa tất cả các Lectures liên quan trong từng Section
            foreach (var section in course.Sections)
            {
                foreach (var lecture in section.Lectures)
                {
                    // Xóa video file trên Google Drive nếu có
                    if (!string.IsNullOrEmpty(lecture.Video_url))
                    {
                        var videoFileId = ExtractFileIdFromUrl(lecture.Video_url);
                        await _googleDriveService.DeleteFileAsync(videoFileId);
                    }

                    // Xóa tài liệu file trên Google Drive nếu có
                    if (!string.IsNullOrEmpty(lecture.Document_url))
                    {
                        var documentFileId = ExtractFileIdFromUrl(lecture.Document_url);
                        await _googleDriveService.DeleteFileAsync(documentFileId);
                    }
                }

                // Xóa Lectures trong Section
                _context.Lectures.RemoveRange(section.Lectures);
            }

            // Xóa Sections
            _context.Sections.RemoveRange(course.Sections);

            // Xóa khóa học
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa khóa học thành công.";
            return RedirectToAction("ListCourses");
        }

        public async Task<IActionResult> DetailsCourses(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Sections)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        public IActionResult CreateSection(int courseId)
        {
            ViewBag.CourseId = courseId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSection(Sections section)
        {
            if (ModelState.IsValid)
            {
                _context.Sections.Add(section);
                await _context.SaveChangesAsync();
                return RedirectToAction("DetailsCourses", new { id = section.CoursesId }); // Chuyển đến chi tiết khóa học
            }

            return View(section);
        }
        public async Task<IActionResult> EditSection(int id)
        {
            var sections = await _context.Sections.SingleOrDefaultAsync(x => x.Id == id);
            if (sections == null)
            {
                return NotFound();
            }
            return View(sections);
        }
        [HttpPost]
        public async Task<IActionResult> EditSection(Sections sections)
        {
            if (ModelState.IsValid)
            {
                _context.Sections.Update(sections);
                await _context.SaveChangesAsync();
                return RedirectToAction("DetailsCourses", new { id = sections.CoursesId }); ;
            }
            return View(sections);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteSection(int id)
        {
            var section = await _context.Sections
                .Include(s => s.Lectures) // Bao gồm các Lecture liên quan
                .FirstOrDefaultAsync(s => s.Id == id);

            if (section == null)
            {
                return NotFound();
            }

            // Xóa các Lecture liên quan
            foreach (var lecture in section.Lectures)
            {
                // Xóa video file trên Google Drive nếu có
                if (!string.IsNullOrEmpty(lecture.Video_url))
                {
                    var videoFileId = ExtractFileIdFromUrl(lecture.Video_url);
                    await _googleDriveService.DeleteFileAsync(videoFileId);
                }

                // Xóa tài liệu file trên Google Drive nếu có
                if (!string.IsNullOrEmpty(lecture.Document_url))
                {
                    var documentFileId = ExtractFileIdFromUrl(lecture.Document_url);
                    await _googleDriveService.DeleteFileAsync(documentFileId);
                }
            }

            // Xóa Lectures khỏi cơ sở dữ liệu
            _context.Lectures.RemoveRange(section.Lectures);

            // Xóa Section
            _context.Sections.Remove(section);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa phần và các bài giảng liên quan thành công.";
            return RedirectToAction("DetailsCourses", new { id = section.CoursesId });
        }
        public async Task<IActionResult> DetailsSection(int sectionId)
        {
            var section = await _context.Sections
                .Include(s => s.Lectures)
                .FirstOrDefaultAsync(s => s.Id == sectionId);
            ViewBag.CourseId = section.CoursesId;

            return View(section);
        }
        public IActionResult CreateLecture(int sectionId)
        {
            ViewBag.SectionId = sectionId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLecture(Lectures lecture, int courseId, IFormFile? videoFile, IFormFile? documentFile)
        {
            if (ModelState.IsValid)
            {
                // Upload video lên Google Drive và lấy URL nhúng
                if (videoFile != null && videoFile.Length > 0)
                {
                    var videoFileId = await _googleDriveService.UploadFileAsync(videoFile);
                    lecture.Video_url = $"https://drive.google.com/file/d/{videoFileId}/preview"; // URL nhúng cho video
                }

                // Upload tài liệu lên Google Drive và lấy URL tải về
                if (documentFile != null && documentFile.Length > 0)
                {
                    var documentFileId = await _googleDriveService.UploadFileAsync(documentFile);
                    lecture.Document_url = $"https://drive.google.com/uc?id={documentFileId}"; // URL tải về cho tài liệu
                }

                // Lưu Lecture vào cơ sở dữ liệu
                _context.Lectures.Add(lecture);
                await _context.SaveChangesAsync();

                // Chuyển hướng đến danh sách bài giảng trong phần chi tiết của Section
                return RedirectToAction("DetailsSection", new { sectionId = lecture.SectionId });
            }

            return View(lecture);
        }
        public async Task<IActionResult> EditLecture(int id)
        {
            var lecture = await _context.Lectures.FindAsync(id);
            if (lecture == null)
            {
                return NotFound();
            }

            return View(lecture);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLecture(int id, Lectures lecture, IFormFile videoFile, IFormFile documentFile)
        {
            if (id != lecture.Id)
            {
                return BadRequest();
            }

            // Lấy bản ghi lecture hiện tại từ cơ sở dữ liệu
            var existingLecture = await _context.Lectures.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
            if (existingLecture == null)
            {
                return NotFound();
            }

            // Cập nhật các thông tin không phải là file video và tài liệu
            existingLecture.Name = lecture.Name;
            existingLecture.Description = lecture.Description;
            existingLecture.CreateDate = lecture.CreateDate;


            // Kiểm tra và cập nhật video nếu có file video mới
            if (videoFile != null && videoFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(existingLecture.Video_url))
                {
                    var oldVideoFileId = ExtractFileIdFromUrl(existingLecture.Video_url);
                    await _googleDriveService.DeleteFileAsync(oldVideoFileId);
                }

                var videoFileId = await _googleDriveService.UploadFileAsync(videoFile);
                existingLecture.Video_url = $"https://drive.google.com/file/d/{videoFileId}/preview";
            }

            // Kiểm tra và cập nhật tài liệu nếu có file tài liệu mới
            if (documentFile != null && documentFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(existingLecture.Document_url))
                {
                    var oldDocumentFileId = ExtractFileIdFromUrl(existingLecture.Document_url);
                    await _googleDriveService.DeleteFileAsync(oldDocumentFileId);
                }

                var documentFileId = await _googleDriveService.UploadFileAsync(documentFile);
                existingLecture.Document_url = $"https://drive.google.com/uc?id={documentFileId}";
            }

            // Cập nhật Lecture trong cơ sở dữ liệu
            _context.Update(existingLecture);
            await _context.SaveChangesAsync();

            // Chuyển hướng đến chi tiết phần chứa bài giảng
            return RedirectToAction("DetailsSection", new { sectionId = existingLecture.SectionId });
        }



        public async Task<IActionResult> DeleteLecture(int id)
        {
            var lecture = await _context.Lectures.FindAsync(id);

            if (lecture == null)
            {
                return NotFound();
            }

            // Xóa video file trên Google Drive nếu có
            if (!string.IsNullOrEmpty(lecture.Video_url))
            {
                var videoFileId = ExtractFileIdFromUrl(lecture.Video_url);
                await _googleDriveService.DeleteFileAsync(videoFileId);
            }

            // Xóa tài liệu file trên Google Drive nếu có
            if (!string.IsNullOrEmpty(lecture.Document_url))
            {
                var documentFileId = ExtractFileIdFromUrl(lecture.Document_url);
                await _googleDriveService.DeleteFileAsync(documentFileId);
            }

            // Xóa Lecture khỏi cơ sở dữ liệu
            _context.Lectures.Remove(lecture);
            await _context.SaveChangesAsync();

            // Chuyển hướng về danh sách bài giảng
            return RedirectToAction("DetailsSection", new { sectionId = lecture.SectionId });
        }

        // GET: CreateFullQuiz - kiểm tra nếu quiz đã tồn tại
        public async Task<IActionResult> CreateFullQuiz(int lectureId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(q => q.LectureId == lectureId);
            var lecture = await _context.Lectures.FirstOrDefaultAsync(q=> q.Id == lectureId);
            if (quiz == null)
            {
                quiz = new Quiz { LectureId = lectureId, Title = "Quiz của "+ lecture.Name };
                _context.Quizzes.Add(quiz);
                await _context.SaveChangesAsync();
            }

            return View(quiz);
        }


        // POST: Thêm câu hỏi vào quiz
        [HttpPost]
        public async Task<IActionResult> AddQuestion(int quizId, string questionText)
        {
            var question = new Question { QuizId = quizId, Text = questionText };
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
            return Json(new { success = true, questionId = question.Id, questionText = question.Text });
        }

        // POST: Cập nhật nội dung câu hỏi
        [HttpPost]
        public async Task<IActionResult> UpdateQuestion(int questionId, string newText)
        {
            var question = await _context.Questions.FindAsync(questionId);
            if (question == null)
            {
                return Json(new { success = false, message = "Câu hỏi không tồn tại." });
            }

            question.Text = newText;
            _context.Questions.Update(question);
            await _context.SaveChangesAsync();
            return Json(new { success = true, newText = question.Text });
        }

        // POST: Xóa câu hỏi và tất cả đáp án liên quan
        [HttpPost]
        public async Task<IActionResult> DeleteQuestion(int questionId)
        {
            var question = await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
            {
                return Json(new { success = false, message = "Câu hỏi không tồn tại." });
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // POST: Thêm đáp án cho câu hỏi
        [HttpPost]
        public async Task<IActionResult> AddAnswer(int questionId, string answerText, bool isCorrect)
        {
            var answer = new Answer { QuestionId = questionId, Text = answerText, IsCorrect = isCorrect };

            // Nếu là đáp án đúng, đặt tất cả đáp án khác của câu hỏi thành sai
            if (isCorrect)
            {
                var otherAnswers = _context.Answers.Where(a => a.QuestionId == questionId);
                foreach (var otherAnswer in otherAnswers)
                {
                    otherAnswer.IsCorrect = false;
                }
            }

            _context.Answers.Add(answer);
            await _context.SaveChangesAsync();
            return Json(new { success = true, answerId = answer.Id, answerText = answer.Text, isCorrect = answer.IsCorrect });
        }

        // POST: Cập nhật đáp án, chỉ một đáp án đúng cho mỗi câu hỏi
        [HttpPost]
        public async Task<IActionResult> UpdateAnswer(int answerId, string newText, bool isCorrect)
        {
            var answer = await _context.Answers.FindAsync(answerId);
            if (answer == null)
            {
                return Json(new { success = false, message = "Đáp án không tồn tại." });
            }

            answer.Text = newText;
            answer.IsCorrect = isCorrect;

            // Nếu đáp án này được đánh dấu là đúng, đặt tất cả đáp án khác của câu hỏi thành sai
            if (isCorrect)
            {
                var otherAnswers = _context.Answers.Where(a => a.QuestionId == answer.QuestionId && a.Id != answerId);
                foreach (var otherAnswer in otherAnswers)
                {
                    otherAnswer.IsCorrect = false;
                }
            }

            _context.Answers.Update(answer);
            await _context.SaveChangesAsync();
            return Json(new { success = true, newText = answer.Text, isCorrect = answer.IsCorrect });
        }

        // POST: Xóa đáp án
        [HttpPost]
        public async Task<IActionResult> DeleteAnswer(int answerId)
        {
            var answer = await _context.Answers.FindAsync(answerId);
            if (answer == null)
            {
                return Json(new { success = false, message = "Đáp án không tồn tại." });
            }

            _context.Answers.Remove(answer);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]

        public async Task<IActionResult> SubmitCourseForApproval(int id)
        {
            var userId = _userManager.GetUserId(User);

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (course == null)
            {
                return NotFound("Khóa học không tồn tại hoặc bạn không có quyền truy cập.");
            }

            course.Status = CourseStatus.UnderReview; // Chuyển sang trạng thái "Đang xét duyệt"
            course.SubmittedDate = DateTime.UtcNow;

            _context.Courses.Update(course);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Khóa học đã được gửi xét duyệt.";
            return RedirectToAction("ListCourses");
        }

    }
}
