using DoAnChuyennNganh.Data;
using DoAnChuyennNganh.Models;
using DoAnChuyennNganh.Services.Email;
using Google.Apis.Drive.v3.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DoAnChuyennNganh.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager, IEmailService emailService)
        {
            _emailService = emailService;
            _roleManager = roleManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> QuanLySubject()
        {
            var subjects = await _context.Subjects.ToListAsync();
            return View(subjects);
        }
        public IActionResult CreateSubject()
        {
            return View();
        }

        // POST: Subject/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubject(Subject subject)
        {
            if (ModelState.IsValid)
            {
                _context.Add(subject);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(subject);
        }
        public async Task<IActionResult> EditSubject(int id)
        {
            var subject = await _context.Subjects.SingleOrDefaultAsync(x => x.Id == id);
            if (subject == null)
            {
                return NotFound();
            }
            return View(subject);
        }
        [HttpPost]
        public async Task<IActionResult> EditSubject(Subject subject)
        {
            if (ModelState.IsValid)
            {
                _context.Subjects.Update(subject);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(QuanLySubject));
            }
            return View(subject);
        }
        public async Task<IActionResult> QuanLyUser(string searchQuery, string roleFilter, int page = 1, int pageSize = 5)
        {
            var query = _context.Users.AsQueryable();

            // Tìm kiếm theo tên (FirstName hoặc LastName)
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(u => u.FirstName.Contains(searchQuery) || u.LastName.Contains(searchQuery));
            }

            // Lọc theo vai trò
            if (!string.IsNullOrEmpty(roleFilter))
            {
                query = query.Join(
                    _context.UserRoles, // Bảng liên kết
                    user => user.Id, // Khóa ngoại trong bảng Users
                    userRole => userRole.UserId, // Khóa ngoại trong bảng UserRoles
                    (user, userRole) => new { user, userRole } // Tạo một đối tượng tạm thời
                )
                .Join(
                    _context.Roles, // Bảng Roles
                    combined => combined.userRole.RoleId, // Khóa ngoại trong bảng UserRoles
                    role => role.Id, // Khóa chính trong bảng Roles
                    (combined, role) => new { combined.user, role }
                )
                .Where(combined => combined.role.Name == roleFilter)
                .Select(combined => combined.user); // Chọn lại User
            }
            var totalUsers = await query.AsNoTracking().CountAsync();

            // Phân trang
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lấy vai trò của từng user
            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles;
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.SearchQuery = searchQuery;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.Roles = await _context.Roles.Select(r => r.Name).ToListAsync(); // Lấy danh sách role

            return View(users);
        }

        public async Task<IActionResult> Manage(string userId)
        {
            ViewBag.userId = userId;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return View("NotFound");
            }
            ViewBag.UserName = user.UserName;
            var model = new List<ManageUserRolesViewModel>();
            foreach (var role in _roleManager.Roles)
            {
                var userRolesViewModel = new ManageUserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name
                };
                if (await _userManager.IsInRoleAsync(user, role.Name))
                {
                    userRolesViewModel.Selected = true;
                }
                else
                {
                    userRolesViewModel.Selected = false;
                }
                model.Add(userRolesViewModel);
            }
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Manage(string SelectedRole, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View();
            }

            // Xóa tất cả các vai trò hiện tại của người dùng
            var roles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, roles);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Không thể xóa vai trò hiện tại của người dùng");
                return View();
            }

            // Thêm vai trò mới đã chọn
            result = await _userManager.AddToRoleAsync(user, SelectedRole);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Không thể thêm vai trò đã chọn cho người dùng");
                return View();
            }

            return RedirectToAction("QuanLyUser");
        }
        [HttpGet]
        public async Task<IActionResult> ReviewApplications()
        {
            var applications = await _context.TeacherApplications
                .Include(a => a.User)
                .Include(a => a.ApplicationFiles)
                .ToListAsync();

            return View(applications);
        }
        [HttpPost]
        public async Task<IActionResult> ApproveApplication(int id)
        {
            var application = await _context.TeacherApplications
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            // Cập nhật trạng thái đơn đăng ký
            application.Status = ApplicationStatus.Approved;

            // Chuyển đổi role của người dùng sang "GiangVien"
            var user = application.User;
            if (user != null)
            {
                // Lấy danh sách tất cả các vai trò hiện tại của người dùng
                var currentRoles = await _userManager.GetRolesAsync(user);

                // Xóa tất cả các vai trò hiện tại
                var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeRolesResult.Succeeded)
                {
                    ModelState.AddModelError("", "Không thể xóa vai trò hiện tại của người dùng.");
                    return View("ReviewApplications", await _context.TeacherApplications.ToListAsync());
                }

                // Gán vai trò mới "GiangVien"
                var addRoleResult = await _userManager.AddToRoleAsync(user, "GiangVien");
                if (!addRoleResult.Succeeded)
                {
                    ModelState.AddModelError("", "Không thể gán vai trò Giảng viên.");
                    return View("ReviewApplications", await _context.TeacherApplications.ToListAsync());
                }
            }

            // Lưu thay đổi vào cơ sở dữ liệu
            _context.TeacherApplications.Update(application);
            await _context.SaveChangesAsync();

            // Gửi email thông báo
            await _emailService.SendEmailAsync(
                application.Email,
                "Thông báo: Đơn đăng ký giảng viên đã được phê duyệt",
                $@"
            <p>Kính gửi <strong>{application.FullName}</strong>,</p>
            <p>Chúng tôi xin chúc mừng bạn! Đơn đăng ký làm giảng viên của bạn đã được phê duyệt.</p>
            <p>Hãy đăng nhập vào hệ thống để bắt đầu sử dụng các tính năng dành cho giảng viên. Nếu có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi.</p>
            <p>Trân trọng,<br />
            Đội ngũ quản lý hệ thống</p>
        ");

            return RedirectToAction("ReviewApplications");
        }
        [HttpPost]
        public async Task<IActionResult> RejectApplication(int id)
        {
            var application = await _context.TeacherApplications
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            // Cập nhật trạng thái đơn đăng ký
            application.Status = ApplicationStatus.Rejected;

            // Lưu thay đổi vào cơ sở dữ liệu
            _context.TeacherApplications.Update(application);
            await _context.SaveChangesAsync();

            // Gửi email thông báo
            await _emailService.SendEmailAsync(
                application.Email,
                "Thông báo: Đơn đăng ký giảng viên bị từ chối",
                $@"
            <p>Kính gửi <strong>{application.FullName}</strong>,</p>
            <p>Chúng tôi rất tiếc phải thông báo rằng đơn đăng ký làm giảng viên của bạn đã bị từ chối.</p>
            <p>Để đăng ký lại, vui lòng kiểm tra lại thông tin và nộp đơn mới nếu bạn cảm thấy phù hợp. Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi.</p>
            <p>Trân trọng,<br />
            Đội ngũ quản lý hệ thống</p>
        ");

            return RedirectToAction("ReviewApplications");
        }
        // Hiển thị danh sách khóa học đang xét duyệt
        public async Task<IActionResult> PendingCourses()
        {
            var courses = await _context.Courses
                .Include(c => c.User) // Include thông tin giảng viên
                .Where(c => c.Status == CourseStatus.UnderReview)
                .ToListAsync();

            return View(courses);
        }

        // Hiển thị danh sách khóa học đã xét duyệt
        public async Task<IActionResult> ApprovedCourses()
        {
            var courses = await _context.Courses
                .Include(c => c.User) // Include thông tin giảng viên
                .Where(c => c.Status == CourseStatus.Approved || c.Status == CourseStatus.Hiden)
                .ToListAsync();

            return View(courses);
        }

        // Chấp nhận khóa học
        [HttpPost]
        public async Task<IActionResult> ApproveCourse(int id)
        {
            var course = await _context.Courses.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound("Khóa học không tồn tại.");
            }

            course.Status = CourseStatus.Approved; // Chuyển trạng thái thành Active
            course.SubmittedDate = DateTime.Now;
            _context.Courses.Update(course);
            await _context.SaveChangesAsync();

            // Gửi email thông báo
            if (!string.IsNullOrEmpty(course.User?.Email))
            {
                var subject = "Khóa học của bạn đã được phê duyệt";
                var body = $"<p>Chào {course.User.LastName+' ' +course.User.FirstName},</p>" +
                           $"<p>Khóa học <strong>{course.Name}</strong> của bạn đã được phê duyệt và hiện đang hiển thị trên hệ thống.</p>";
                await _emailService.SendEmailAsync(course.User.Email, subject, body);
            }

            TempData["SuccessMessage"] = "Khóa học đã được phê duyệt.";
            return RedirectToAction("PendingCourses");
        }

        // Từ chối khóa học
        [HttpPost]
        public async Task<IActionResult> RejectCourse(int id)
        {
            var course = await _context.Courses.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound("Khóa học không tồn tại.");
            }

            course.Status = CourseStatus.Pending;
            _context.Courses.Update(course);
            await _context.SaveChangesAsync();

            // Gửi email thông báo
            if (!string.IsNullOrEmpty(course.User?.Email))
            {
                var subject = "Khóa học của bạn đã bị từ chối";
                var body = $"<p>Chào {course.User.LastName + ' ' + course.User.FirstName},</p>" +
                           $"<p>Khóa học <strong>{course.Name}</strong> của bạn đã bị từ chối. Vui lòng kiểm tra lại thông tin hoặc liên hệ quản trị viên để biết thêm chi tiết.</p>";
                await _emailService.SendEmailAsync(course.User.Email, subject, body);
            }

            TempData["WarningMessage"] = "Khóa học đã bị từ chối.";
            return RedirectToAction("PendingCourses");
        }

        // Ẩn khóa học
        [HttpPost]
        public async Task<IActionResult> HideCourse(int id)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound("Khóa học không tồn tại.");
            }

            course.Status = CourseStatus.Hiden;
            _context.Courses.Update(course);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Khóa học đã được ẩn.";
            return RedirectToAction("ApprovedCourses");
        }

        // Hủy ẩn khóa học
        [HttpPost]
        public async Task<IActionResult> UnhideCourse(int id)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound("Khóa học không tồn tại.");
            }

            course.Status = CourseStatus.Approved;
            _context.Courses.Update(course);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Khóa học đã được hiển thị lại.";
            return RedirectToAction("ApprovedCourses");
        }


        public IActionResult Statistics()
        {
            // Thống kê số lượng khóa học theo chủ đề
            var courseStats = _context.Courses
                .GroupBy(c => c.Subject.Name) // Nhóm theo tên chủ đề
                .Select(g => new { Subject = g.Key, Count = g.Count() })
                .ToList();

            // Thống kê số lượng người dùng
            var userStats = new
            {
                Teachers = _context.UserRoles.Count(ur => ur.RoleId == _context.Roles.First(r => r.Name == "GiangVien").Id),
                Students = _context.UserRoles.Count(ur => ur.RoleId == _context.Roles.First(r => r.Name == "HocVien").Id)
            };

            // Thống kê doanh thu theo thời gian
            var revenueStats = _context.vnPays
            .Where(v => v.CreatedDate.HasValue)
            .GroupBy(v => v.CreatedDate.Value.Month)
            .Select(g => new { Month = g.Key, Revenue = g.Sum(v => v.Price) })
            .ToList();

            // Truyền dữ liệu qua ViewBag
            ViewBag.CourseStats = System.Text.Json.JsonSerializer.Serialize(courseStats);
            ViewBag.UserStats = System.Text.Json.JsonSerializer.Serialize(userStats);
            ViewBag.RevenueStats = System.Text.Json.JsonSerializer.Serialize(revenueStats);

            return View();

        }
        [HttpPost]
        public async Task<IActionResult> LockAccount(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            user.Status = "Bị khóa"; // Cập nhật trạng thái
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Tài khoản đã bị khóa.";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi khóa tài khoản.";
            }

            return RedirectToAction("QuanLyUser"); // Quay lại danh sách người dùng
        }
        [HttpPost]
        public async Task<IActionResult> UnlockAccount(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            user.Status = "Đang hoạt động"; // Cập nhật trạng thái
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Tài khoản đã được mở khóa.";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi mở khóa tài khoản.";
            }

            return RedirectToAction("QuanLyUser"); // Quay lại danh sách người dùng
        }

    }
}
