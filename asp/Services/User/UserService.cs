using asp.Helper.ConnectDb;
using asp.Helper.File;
using asp.Models.User;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using System.Net;
using System.Text.RegularExpressions;

namespace asp.Services.User
{


    public class UserService
    {
        private readonly IMongoCollection<Users> _collection;

        public UserService(ConnectDbHelper dbHelper)
        {
            _collection = dbHelper.GetCollection<Users>();
        }
        public async Task<Users> GetByIdAsync(string id)
        {
            try
            {
                var objectId = ObjectId.Parse(id);
                var filter = Builders<Users>.Filter.Eq("_id", objectId);

                // Chỉ lấy các trường không bao gồm password
                //var projection = Builders<Users>.Projection.Exclude("passWord");

                // Dùng projection để loại bỏ passWord và chỉ lấy các trường còn lại
                var result = await _collection.Find(filter)
                                              //.Project<Users>(projection)
                                              .FirstOrDefaultAsync();

                return result; // Trả về kết quả đã loại bỏ password
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Format exception: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving user: {ex.Message}");
                return null;
            }
        }
        // hàm update thông tin user
        public async Task<bool> UpdateAsync(string id, Users updatedEntity)
        {
            if (string.IsNullOrEmpty(id) || updatedEntity == null)
            {
                throw new ArgumentException("Invalid id or entity.");
            }

            // Loại bỏ _id từ updatedEntity trước khi sử dụng
            var updatedEntityDoc = updatedEntity.ToBsonDocument();
            updatedEntityDoc.Remove("_id"); // Xóa trường _id để không cập nhật nó

            // Tạo filter để tìm tài liệu cần cập nhật theo _id
            var filter = Builders<Users>.Filter.Eq("_id", ObjectId.Parse(id));

            // Tạo danh sách các cập nhật
            var updates = new List<UpdateDefinition<Users>>();

            foreach (var element in updatedEntityDoc.Elements)
            {
                updates.Add(Builders<Users>.Update.Set(element.Name, element.Value));
            }

            // Kết hợp các cập nhật thành một UpdateDefinition
            var updateDefinition = Builders<Users>.Update.Combine(updates);

            // Thực hiện cập nhật tài liệu
            var result = await _collection.UpdateOneAsync(filter, updateDefinition);

            return result.MatchedCount > 0;
        }
        // hàm update avatar
        public async Task<bool> UpdateAvatarAsync(string id, IFormFile avatarFile)
        {
            if (string.IsNullOrEmpty(id) || avatarFile == null)
            {
                throw new ArgumentException("Invalid id or avatar file.");
            }

            // Tìm người dùng theo id
            var user = await _collection.Find(Builders<Users>.Filter.Eq("_id", ObjectId.Parse(id))).FirstOrDefaultAsync();

            if (user == null)
            {
                // Nếu không tìm thấy người dùng, trả về lỗi
                return false; // Có thể trả về một mã lỗi tùy theo yêu cầu của ứng dụng
            }

            // Lưu file avatar mới và lấy tên file
            var avatarFileName = await SaveFileHelper.SaveFileAsync(avatarFile);  // Đảm bảo rằng SaveFileAsync trả về tên file hợp lệ

            // Xoá file avatar cũ nếu tồn tại
            if (!string.IsNullOrEmpty(user.avatar))
            {
                // Kiểm tra xem avatar cũ có tồn tại trong hệ thống không
                var oldFilePath = Path.Combine("Files", user.avatar);  // Đường dẫn tới file cũ
                if (File.Exists(oldFilePath))
                {
                    SaveFileHelper.DeleteProjectFile(user.avatar);  // Xóa file cũ
                }
            }

            // Tạo filter để tìm người dùng cần cập nhật theo _id
            var filter = Builders<Users>.Filter.Eq("_id", ObjectId.Parse(id));

            // Tạo định nghĩa cập nhật cho trường avatar
            var update = Builders<Users>.Update.Set("avatar", avatarFileName);

            // Thực hiện cập nhật trường avatar
            var result = await _collection.UpdateOneAsync(filter, update);

            // Kiểm tra nếu có tài liệu nào bị cập nhật
            return result.MatchedCount > 0;
        }

        // lấy list người dùng
        public async Task<List<Users>> GetAllAsync(int skipAmount, int pageSize)
        {
            var sortDefinition = Builders<Users>.Sort.Descending(x => x.Id);

            // Lấy tất cả ProjectFunds với các trang (skip và limit)
            var projectFunds = await _collection.Find(_ => true)
                                                .Skip(skipAmount)
                                                .Sort(sortDefinition)
                                                .Limit(pageSize)
                                                .ToListAsync();

            return projectFunds;
        }

        public async Task<long> CountAsync()
        {
            return await _collection.CountDocumentsAsync(_ => true);
        }
        // xoá danh sách các người dùng
        public async Task<long> DeleteByIdsAsync(List<string> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                throw new ArgumentException("The list of ids cannot be null or empty.");
            }

            var objectIdList = new List<ObjectId>();

            foreach (var id in ids)
            {
                if (ObjectId.TryParse(id, out var objectId))
                {
                    // Tạo filter để tìm tài liệu cần cập nhật theo _id
                    var filterFund = Builders<Users>.Filter.Eq("_id", ObjectId.Parse(id));

                    // Tìm tài liệu trước khi cập nhật để lấy ảnh cũ
                    var existingEntity = await _collection.Find(filterFund).FirstOrDefaultAsync();
                    // Nếu có ảnh cũ trong cơ sở dữ liệu và có ảnh mới, xóa ảnh cũ
                    if (existingEntity != null && !string.IsNullOrEmpty(existingEntity.avatar))
                    {
                        // Xóa ảnh cũ
                        SaveFileHelper.DeleteProjectFile(existingEntity.avatar);
                    }

                    objectIdList.Add(objectId);
                }
                else
                {
                    throw new ArgumentException($"Invalid id format: {id}");
                }
            }

            var filter = Builders<Users>.Filter.In("_id", objectIdList);
            var result = await _collection.DeleteManyAsync(filter);

            return result.DeletedCount;
        }

        //// lưu file vào server khi đẩy lên
        //private async Task<string> SaveFileAsync(IFormFile file)
        //{
        //    // Mã lưu file giữ nguyên không đổi
        //    var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Files");

        //    // Tạo thư mục lưu trữ nếu chưa tồn tại
        //    if (!Directory.Exists(uploadFolder))
        //    {
        //        Directory.CreateDirectory(uploadFolder);
        //    }

        //    // Lấy tên file không kèm đuôi mở rộng
        //    var fileName = Path.GetFileNameWithoutExtension(file.FileName);

        //    // Lấy đuôi mở rộng của file
        //    var fileExtension = Path.GetExtension(file.FileName);

        //    // Tạo tên file mới để tránh trùng lặp
        //    var uniqueFileName = $"{fileName}_{DateTime.Now:yyyyMMddHHmmssfff}{fileExtension}";

        //    var filePath = Path.Combine(uploadFolder, uniqueFileName);

        //    // Lưu file vào đường dẫn
        //    using (var stream = new FileStream(filePath, FileMode.Create))
        //    {
        //        await file.CopyToAsync(stream); // Sử dụng CopyToAsync để đợi hoàn tất
        //    }

        //    return uniqueFileName;
        //}
        //// xoá file
        //private void DeleteProjectFile(Users user)
        //{
        //    // Kiểm tra xem project có file liên quan không
        //    if (!string.IsNullOrEmpty(user.avatar))
        //    {
        //        // Đường dẫn đến file
        //        string filePath = Path.Combine("Files", user.avatar);

        //        try
        //        {
        //            // Xóa file từ hệ thống tệp
        //            File.Delete(filePath);

        //            Console.WriteLine($"Đã xóa file: {user.avatar}");
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Lỗi khi xóa file: {ex.Message}");
        //        }
        //    }
        //}

        //        private readonly IMongoCollection<Users> _collection;

        //        public UserService(IOptions<MongoDbSetting> databaseSettings)
        //        {
        //            var client = new MongoClient(databaseSettings.Value.ConnectionURI);
        //            var database = client.GetDatabase(databaseSettings.Value.DatabaseName);
        //            _collection = database.GetCollection<Users>(typeof(Users).Name.ToLower());
        //        }

        //        public async Task<List<Users>> GetAllAsync(int skipAmount, int pageSize)
        //        {
        //            var sortDefinition = Builders<Users>.Sort.Descending(x => x.Id); 

        //            return await _collection.Find(_ => true)
        //                                    .Skip(skipAmount)
        //                                    .Sort(sortDefinition)
        //                                    .Limit(pageSize)
        //                                    .ToListAsync();
        //        }

        //        
        //        public async Task<Users?> GetByTenDangNhapAsync(string tendangnhap) =>
        //            await _collection.Find(Builders<Users>.Filter.Eq("tendangnhap", tendangnhap)).FirstOrDefaultAsync();
        //        public async Task<List<Users>> GetByIdDepartmentAsync(string id) =>
        //           await _collection.Find(Builders<Users>.Filter.Eq("id_khoa", id)).ToListAsync();

        //        public async Task<Users> GetUserByTenDangNhapAndPassword(string tendangnhap, string matkhau)
        //        {
        //            var filter = Builders<Users>.Filter.And(
        //                Builders<Users>.Filter.Eq("tendangnhap", tendangnhap),
        //                Builders<Users>.Filter.Eq("matkhau", matkhau)
        //            );

        //            var user = await _collection.Find(filter).FirstOrDefaultAsync();

        //            return user;
        //        }


        //        public async Task CreateAsync(Users newEntity)
        //        {
        //            await _collection.InsertOneAsync(newEntity);
        //        }
        //        public async Task<long> CreatetManyAsync(List<Users> entities)
        //        {
        //            try
        //            {
        //                await _collection.InsertManyAsync(entities);
        //                return entities.Count;
        //            }
        //            catch (Exception ex)
        //            {
        //                throw new Exception("Lỗi khi chèn nhiều người dùng.", ex);
        //            }
        //        }

        //public async Task UpdateAsync(string id, Users updatedEntity)
        //{
        //    var filter = Builders<Users>.Filter.Eq("_id", ObjectId.Parse(id));
        //    await _collection.ReplaceOneAsync(filter, updatedEntity);
        //}



        //public async Task RemoveAsync(string id)
        //{
        //    var filter = Builders<Users>.Filter.Eq("_id", ObjectId.Parse(id));
        //    await _collection.DeleteOneAsync(filter);
        //}
        //public async Task<long> DeleteByIdsAsync(List<string> ids)
        //{
        //    var filter = Builders<Users>.Filter.In("_id", ids.Select(ObjectId.Parse));
        //    var result = await _collection.DeleteManyAsync(filter);
        //    return result.DeletedCount;
        //}


    }
}

