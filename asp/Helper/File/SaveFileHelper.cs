using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
namespace asp.Helper.File
{
    public class SaveFileHelper
    {
        // lưu file vào server khi đẩy lên
        public static async Task<string> SaveFileAsync(IFormFile file)
        {
            // Mã lưu file giữ nguyên không đổi
            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Files");

            // Tạo thư mục lưu trữ nếu chưa tồn tại
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // Lấy tên file không kèm đuôi mở rộng
            var fileName = Path.GetFileNameWithoutExtension(file.FileName);

            // Lấy đuôi mở rộng của file
            var fileExtension = Path.GetExtension(file.FileName);

            // Tạo tên file mới để tránh trùng lặp
            var uniqueFileName = $"{fileName}_{DateTime.Now:yyyyMMddHHmmssfff}{fileExtension}";

            var filePath = Path.Combine(uploadFolder, uniqueFileName);

            // Lưu file vào đường dẫn
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream); // Sử dụng CopyToAsync để đợi hoàn tất
            }

            return uniqueFileName;
        }
        // xoá file
        public static void DeleteProjectFile(string filePathDe)
        {
            // Kiểm tra xem project có file liên quan không
            if (!string.IsNullOrEmpty(filePathDe))
            {
                // Đường dẫn đến file
                string filePath = Path.Combine("Files", filePathDe);

                try
                {
                    // Xóa file từ hệ thống tệp
                    //File.Delete(filePath);
                    System.IO.File.Delete(filePath);
                    Console.WriteLine($"Đã xóa file: {filePathDe}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi xóa file: {ex.Message}");
                }
            }
        }
    }
}
