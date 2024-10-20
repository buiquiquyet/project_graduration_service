using asp.DTO;
using asp.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text.RegularExpressions;

namespace asp.Respositories
{


    public class FileService
    {
        private readonly IMongoCollection<Files> _collection;

        public FileService(IOptions<MongoDbSetting> databaseSettings)
        {
            var client = new MongoClient(databaseSettings.Value.ConnectionURI);
            var database = client.GetDatabase(databaseSettings.Value.DatabaseName);
            _collection = database.GetCollection<Files>(typeof(Files).Name.ToLower());
        }

        public async Task<long> CountAsync()
        {
            return await _collection.CountDocumentsAsync(_ => true);
        }
        public async Task<List<Files>> GetByRecordIdAsync(string idRecord)
        {
            var filter = Builders<Files>.Filter.Eq("profile_id", idRecord);
            return await _collection.Find(filter).ToListAsync();
        }
        public async Task<Files?> GetByIdAsync(string id) =>
            await _collection.Find(Builders<Files>.Filter.Eq("_id", ObjectId.Parse(id))).FirstOrDefaultAsync();

        public async Task CreateAsync(FileDTO newEntities)
        {
            try
            {
               /* foreach (var entity in newEntities)
                {*/
                    if (newEntities.ten != null)
                    {
                        var fileName = await SaveFileAsync(newEntities.ten);

                        var fileToAdd = new Files
                        {
                            profile_id = newEntities.profile_id,
                            ten = fileName
                        };
                        await _collection.InsertOneAsync(fileToAdd);
                    }
                

            }
            catch (Exception ex)
            {
              
            }
        }
        public async Task UpdateAsync(string id, Files updatedEntity)
        {
            var filter = Builders<Files>.Filter.Eq("_id", ObjectId.Parse(id));
            await _collection.ReplaceOneAsync(filter, updatedEntity);
        }

        public async Task RemoveAsync(string id)
        {
            var filter = Builders<Files>.Filter.Eq("_id", ObjectId.Parse(id));
            await _collection.DeleteOneAsync(filter);
        }
        public async Task<long> DeleteByIdsAsync(List<string> ids)
        {
            var existingFiles = await _collection.Find(u => ids.Any(id => id == u.Id)).ToListAsync();
            if ( existingFiles.Count == 0)
            {   
                return 0;
            }
            foreach(var file in existingFiles)
            {
                DeleteProjectFile(file);
            }
            var filter = Builders<Files>.Filter.In("_id", ids.Select(ObjectId.Parse));
            var result = await _collection.DeleteManyAsync(filter);
            return result.DeletedCount;
        }
        public async Task<long> DeleteByProfileIdsAsync(List<string> profileIds)
        {
            var existingFiles = await _collection.Find(u => profileIds.Any(id => id == u.profile_id)).ToListAsync();
            if (existingFiles.Count == 0)
            {
                return 0;
            }
            foreach (var file in existingFiles)
            {
                DeleteProjectFile(file);
            }
            var filter = Builders<Files>.Filter.In("profile_id", profileIds);
            var result = await _collection.DeleteManyAsync(filter);
            return result.DeletedCount;
        }
        private async Task<string> SaveFileAsync(IFormFile file)
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
        private void DeleteProjectFile(Files file)
        {
            // Kiểm tra xem project có file liên quan không
            if (!string.IsNullOrEmpty(file.ten))
            {
                // Đường dẫn đến file
                string filePath = Path.Combine("Files", file.ten);

                try
                {
                    // Xóa file từ hệ thống tệp
                    File.Delete(filePath);

                    Console.WriteLine($"Đã xóa file: {file.ten}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi xóa file: {ex.Message}");
                }
            }
        }
    }
}

