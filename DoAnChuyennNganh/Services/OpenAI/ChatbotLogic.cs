using Microsoft.AspNetCore.Cors.Infrastructure;

namespace DoAnChuyennNganh.Services.OpenAI
{
    public class ChatbotLogic
    {
        private readonly OpenAiService _openAiService;
        private readonly CourseService _courseService;
        private static readonly Dictionary<string, string> _responseCache = new();

        public ChatbotLogic(OpenAiService openAiService, CourseService courseService)
        {
            _openAiService = openAiService;
            _courseService = courseService;
        }

        public async Task<string> GetChatResponse(string userQuery)
        {
            /* if (_responseCache.ContainsKey(userQuery))
             {
                 return _responseCache[userQuery]; // Trả về từ cache
             }

             string response = await _openAiService.GetChatbotResponse(userQuery);

             _responseCache[userQuery] = response; // Lưu vào cache
             return response;*/

            var coursesContext = await _courseService.GetCoursesAsTextAsync();

            // Tạo prompt ngữ cảnh
            var prompt = $"Bạn là một nhân viên tư vấn khóa học cho website của chúng tôi.website của chúng tôi là một website hỗ trợ học tập trực tuyến bằng cách cung cấp các khóa học online cho mọi người. Dưới đây là danh sách khóa học hiện có:\n" +
                         $"{coursesContext}\n" +
                         $"Người dùng có thể mua (đăng ký tham gia) khóa học bằng cách đăng nhập tài khoản , lựa chọn một khóa học muốn mua rồi chọn phương thức thanh toán(hiên tại chỉ hỗ trợ thanh toán bằng VNPay) sau khi thanh toán thành công khóa học sẽ có trong danh sách khóa học đã mua của người dùng" +
                         $"Người dùng hỏi: {userQuery}\n" +
                         $"Hãy từ chối khéo những câu hỏi không liên quan đến khóa học"+
                         $"Hãy trả lời một cách chuyên nghiệp và cụ thể.";

            // Gửi yêu cầu đến OpenAI với ngữ cảnh
            return await _openAiService.GetChatbotResponse(prompt);
        }
    }

}
