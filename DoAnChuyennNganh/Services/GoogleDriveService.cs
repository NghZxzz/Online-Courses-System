using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace DoAnChuyennNganh.Services
{
    public class GoogleDriveService
    {
        private readonly DriveService _driveService;

        public GoogleDriveService(DriveService driveService)
        {
            _driveService = driveService;
        }
        public GoogleDriveService(IConfiguration configuration)
        {
            var credential = GoogleCredential.FromFile("service_account_key.json") // Đường dẫn tới file JSON
                .CreateScoped(DriveService.ScopeConstants.Drive);

            _driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "E-Learning Platform"
            });
        }
        public async Task<string> UploadFileAsync(IFormFile file)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = file.FileName,
                Parents = new List<string> { "1kKRLLdJa-m7513MYueJIMGz0wWrDn3Ne" } // ID thư mục Google Drive
            };

            FilesResource.CreateMediaUpload request;

            using (var stream = file.OpenReadStream())
            {
                request = _driveService.Files.Create(fileMetadata, stream, file.ContentType);
                request.Fields = "id"; // Chỉ lấy ID sau khi upload
                request.ChunkSize = 256 * 1024; // Kích thước khối 256 KB

                try
                {
                    await request.UploadAsync();
                }
                catch (Exception ex)
                {
                    throw new Exception("Error uploading file: " + ex.Message);
                }
            }

            var fileId = request.ResponseBody?.Id;

            if (!string.IsNullOrEmpty(fileId))
            {
                // Gọi phương thức MakeFilePublicAsync để public file
                await MakeFilePublicAsync(fileId);
                return fileId; // Trả về ID file đã public
            }

            return null;
        }

        public async Task DeleteFileAsync(string fileId)
        {
            try
            {
                await _driveService.Files.Delete(fileId).ExecuteAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error deleting file: " + ex.Message);
            }
        }
        public async Task<string> MakeFilePublicAsync(string fileId)
        {
            var permission = new Google.Apis.Drive.v3.Data.Permission
            {
                Role = "reader",
                Type = "anyone"
            };

            await _driveService.Permissions.Create(permission, fileId).ExecuteAsync();

            return $"https://drive.google.com/uc?id={fileId}";
        }
    }
}
