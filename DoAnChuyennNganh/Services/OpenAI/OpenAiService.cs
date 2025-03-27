using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace DoAnChuyennNganh.Services.OpenAI
{
    public class OpenAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public OpenAiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAi:ApiKey"];
        }

        public async Task<string> GetChatbotResponse(string prompt)
        {
            var requestContent = new
            {
                model = "gpt-3.5-turbo",
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = 500, // Giới hạn số token
                temperature = 0.7
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestContent), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            for (int i = 0; i < 3; i++) // Retry logic
            {
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var responseObject = JsonConvert.DeserializeObject<dynamic>(responseString);
                    return responseObject.choices[0].message.content.ToString();
                }

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    await Task.Delay(2000 * (i + 1)); // Exponential backoff
                    continue;
                }
            }

            throw new Exception("Failed after multiple retries due to rate limiting.");
        }

    }

}
