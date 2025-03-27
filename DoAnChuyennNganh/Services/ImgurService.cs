using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
namespace DoAnChuyennNganh.Services
{
    public class ImgurService
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;

        public ImgurService(HttpClient httpClient, IOptions<ImgurSettings> imgurSettings)
        {
            _httpClient = httpClient;
            _clientId = imgurSettings.Value.ClientId;
        }

        public async Task<string> UploadImageAsync(IFormFile imageFile)
        {
            using var content = new MultipartFormDataContent();
            using var fileStream = imageFile.OpenReadStream();
            var imageContent = new StreamContent(fileStream);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType);
            content.Add(imageContent, "image", imageFile.FileName);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", _clientId);
            var response = await _httpClient.PostAsync("https://api.imgur.com/3/image", content);

            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(jsonResponse);
            var imageUrl = jsonDoc.RootElement.GetProperty("data").GetProperty("link").GetString();

            return imageUrl;
        }
    }
}
