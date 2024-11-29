using asp.Constants;
using asp.Helper;
using asp.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using System.Net;
using System.Numerics;
using System.Text.RegularExpressions;

namespace asp.Respositories
{
   
   
    public class CharityFundService
    {
        private readonly IMongoCollection<CharityFunds> _collection;

        public CharityFundService(ConnectDbHelper dbHelper)
        {
            _collection = dbHelper.GetCollection<CharityFunds>();
        }
        //tạo quỹ
        public async Task<CharityFunds> Create(CharityFunds request)
        {

            // Biến lưu tên file avatar
            string avatarFileName = null;
            if (request.imagesIFormFile != null)
            {
                // Lưu file và lấy tên file
                avatarFileName = await SaveFileHelper.SaveFileAsync(request.imagesIFormFile);
            }
            var registerAuth = new CharityFunds
            {
                email = request.email,
                name = request.name,
                images = avatarFileName,
                description = request.description,
                address = request.address,
                phone = request.phone,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow,
            };

            try
            {
                // Chèn dữ liệu vào collection
                await _collection.InsertOneAsync(registerAuth);
                return registerAuth;
            }
            catch (Exception ex)
            {
                // Xử lý lỗi (ghi log, ném ngoại lệ, v.v.)
                throw new Exception("Có lỗi xảy ra trong quá trình chèn dữ liệu vào cơ sở dữ liệu: " + ex.Message);
            }
        }

        // lấy 1 quỹ
        public async Task<CharityFunds> GetByIdAsync(string id)
        {
            try
            {
                var objectId = ObjectId.Parse(id);
                var filter = Builders<CharityFunds>.Filter.Eq("_id", objectId);

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
        public async Task<bool> UpdateAsync(string id, CharityFunds updatedEntity)
        {
            if (string.IsNullOrEmpty(id) || updatedEntity == null)
            {
                throw new ArgumentException("Invalid id or entity.");
            }

            // Loại bỏ _id từ updatedEntity trước khi sử dụng
            var updatedEntityDoc = updatedEntity.ToBsonDocument();
            updatedEntityDoc.Remove("_id"); // Xóa trường _id để không cập nhật nó

            // Tạo filter để tìm tài liệu cần cập nhật theo _id
            var filter = Builders<CharityFunds>.Filter.Eq("_id", ObjectId.Parse(id));

            // Thực hiện cập nhật tài liệu
            var result = await _collection.UpdateOneAsync(filter, new BsonDocument { { "$set", updatedEntityDoc } });

            return result.MatchedCount > 0;
        }
        // lấy list các quỹ
        public async Task<List<CharityFunds>> GetAllAsync(int skipAmount, int pageSize)
        {
            var sortDefinition = Builders<CharityFunds>.Sort.Descending(x => x.Id);

            return await _collection.Find(_ => true)
                                    .Skip(skipAmount)
                                    .Sort(sortDefinition)
                                    .Limit(pageSize)
                                    .ToListAsync();
        }
        // đếm số lượng bản ghi
        public async Task<long> CountAsync()
        {
            return await _collection.CountDocumentsAsync(_ => true);
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
        //        public async Task<long> CountAsync()
        //        {
        //                return await _collection.CountDocumentsAsync(_ => true);
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

