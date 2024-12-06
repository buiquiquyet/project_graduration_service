using asp.Constants;
using asp.Helper.ConnectDb;
using asp.Helper.File;
using asp.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using System.Net;
using System.Numerics;
using System.Text.RegularExpressions;

namespace asp.Services.Fund
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
        // hàm update thông tin quỹ
        public async Task<bool> UpdateAsync(string id, CharityFunds updatedEntity)
        {
            if (string.IsNullOrEmpty(id) || updatedEntity == null)
            {
                throw new ArgumentException("Invalid id or entity.");
            }

            // Tạo filter để tìm tài liệu cần cập nhật theo _id
            var filter = Builders<CharityFunds>.Filter.Eq("_id", ObjectId.Parse(id));

            // Tìm tài liệu trước khi cập nhật để lấy ảnh cũ
            var existingEntity = await _collection.Find(filter).FirstOrDefaultAsync();

            // Danh sách các cập nhật
            var updates = new List<UpdateDefinition<CharityFunds>>();

            // Tạo một phương thức để thêm các trường vào danh sách cập nhật chỉ khi chúng khác null hoặc không rỗng
            void AddUpdate(Expression<Func<CharityFunds, object>> field, object value)
            {
                if (value != null && !string.IsNullOrEmpty(value.ToString())) // Kiểm tra null hoặc chuỗi rỗng
                {
                    updates.Add(Builders<CharityFunds>.Update.Set(field, value));
                }
            }

            // Thêm các trường vào danh sách cập nhật
            AddUpdate(x => x.name, updatedEntity.name);
            AddUpdate(x => x.email, updatedEntity.email);
            AddUpdate(x => x.phone, updatedEntity.phone);
            AddUpdate(x => x.description, updatedEntity.description);
            AddUpdate(x => x.address, updatedEntity.address);
            AddUpdate(x => x.updatedAt, DateTime.UtcNow);

            // Xử lý ảnh mới nếu có
            if (updatedEntity.imagesIFormFile != null)
            {
                // Lưu ảnh mới và lấy đường dẫn của ảnh
                var newImageFilePath = await SaveFileHelper.SaveFileAsync(updatedEntity.imagesIFormFile);

                // Nếu có ảnh cũ trong cơ sở dữ liệu và có ảnh mới, xóa ảnh cũ
                if (existingEntity != null && !string.IsNullOrEmpty(existingEntity.images))
                {
                    // Xóa ảnh cũ
                    SaveFileHelper.DeleteProjectFile(existingEntity.images);
                }

                // Cập nhật file ảnh mới
                updates.Add(Builders<CharityFunds>.Update.Set(x => x.images, newImageFilePath));
            }

            // Kết hợp tất cả các cập nhật thành một UpdateDefinition
            var updateDefinition = Builders<CharityFunds>.Update.Combine(updates);

            // Thực hiện cập nhật tài liệu
            var result = await _collection.UpdateOneAsync(filter, updateDefinition);

            // Kiểm tra xem có tài liệu nào được cập nhật không
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
        // lấy list các quỹ để fill vào options
        public async Task<List<CharityFundsv2>> GetAllAsyncForOptions(int skipAmount, int pageSize)
        {
            var sortDefinition = Builders<CharityFunds>.Sort.Descending(x => x.Id);

            // Projection chỉ lấy Id và Name
            var projection = Builders<CharityFunds>.Projection
                                                   .Include(cf => cf.Id)  // Bao gồm Id
                                                   .Include(cf => cf.name); // Bao gồm name


            var result = await _collection.Find(_ => true)
                                          .Skip(skipAmount)
                                          .Sort(sortDefinition)
                                          .Limit(pageSize)
                                          .Project<CharityFundsv2>(projection) // Ánh xạ vào DTO
                                          .ToListAsync();

            return result;
        }


        // đếm số lượng bản ghi
        public async Task<long> CountAsync()
        {
            return await _collection.CountDocumentsAsync(_ => true);
        }

        // xoá danh sách các quỹ
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
                    var filterFund = Builders<CharityFunds>.Filter.Eq("_id", ObjectId.Parse(id));

                    // Tìm tài liệu trước khi cập nhật để lấy ảnh cũ
                    var existingEntity = await _collection.Find(filterFund).FirstOrDefaultAsync();
                    // Nếu có ảnh cũ trong cơ sở dữ liệu và có ảnh mới, xóa ảnh cũ
                    if (existingEntity != null && !string.IsNullOrEmpty(existingEntity.images))
                    {
                        // Xóa ảnh cũ
                        SaveFileHelper.DeleteProjectFile(existingEntity.images);
                    }

                    objectIdList.Add(objectId);
                }
                else
                {
                    throw new ArgumentException($"Invalid id format: {id}");
                }
            }

            var filter = Builders<CharityFunds>.Filter.In("_id", objectIdList);
            var result = await _collection.DeleteManyAsync(filter);

            return result.DeletedCount;
        }



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

