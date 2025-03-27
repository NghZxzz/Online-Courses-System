using DoAnChuyennNganh.Services.OpenAI;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Crmf;

namespace DoAnChuyennNganh.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    public class ChatbotController : ControllerBase
    {
        private readonly ChatbotLogic _chatbotLogic;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ChatbotController(ChatbotLogic chatbotLogic, IHttpContextAccessor httpContextAccessor)
        {
            _chatbotLogic = chatbotLogic;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("GetResponse")]
        public async Task<IActionResult> GetResponse([FromBody] ChatRequest request)
        {
            // Gửi câu hỏi tới chatbot và nhận phản hồi
            var botResponse = await _chatbotLogic.GetChatResponse(request.UserQuery);

            // Lấy lịch sử chat từ session
            var session = _httpContextAccessor.HttpContext.Session;
            var chatHistory = session.GetObjectFromJson<List<ChatMessage>>("ChatHistory") ?? new List<ChatMessage>();

            // Thêm đoạn chat mới vào lịch sử
            chatHistory.Add(new ChatMessage
            {
                UserMessage = request.UserQuery,
                BotResponse = botResponse,
                Timestamp = DateTime.Now
            });

            // Lưu lại lịch sử vào session
            session.SetObjectAsJson("ChatHistory", chatHistory);

            return Ok(botResponse);
        }

        [HttpGet("GetChatHistory")]
        public IActionResult GetChatHistory()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var chatHistory = session.GetObjectFromJson<List<ChatMessage>>("ChatHistory") ?? new List<ChatMessage>();

            return Ok(chatHistory);
        }
    }
    public class ChatRequest
    {
        public string UserQuery { get; set; }
    }
    public class ChatMessage
    {
        public string UserMessage { get; set; }
        public string BotResponse { get; set; }
        public DateTime Timestamp { get; set; }
    }


}
