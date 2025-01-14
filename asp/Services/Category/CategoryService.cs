﻿using asp.Constants;
using asp.Helper.ConnectDb;
using asp.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using System.Net;
using System.Numerics;
using System.Text.RegularExpressions;

namespace asp.Services.Category
{


    public class CategoryService
    {
        private readonly IMongoCollection<Categorys> _collection;

        public CategoryService(ConnectDbHelper dbHelper)
        {
            _collection = dbHelper.GetCollection<Categorys>();
        }
        //tạo quỹ
        public async Task<Categorys> Create(Categorys request)
        {



            // Tạo đối tượng ProjectFunds từ dữ liệu request
            var registerAuth = new Categorys
            {
                name = request.name,

                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow,
            };

            try
            {
                // Chèn dữ liệu vào collection MongoDB
                await _collection.InsertOneAsync(registerAuth);

                // Trả về đối tượng đã chèn, bao gồm cả Id từ MongoDB nếu có
                return registerAuth;
            }
            catch (Exception ex)
            {
                // Xử lý lỗi (ghi log, ném ngoại lệ, v.v.)
                // Ví dụ: ghi log chi tiết (có thể dùng log framework như NLog, Serilog)
                throw new Exception("Có lỗi xảy ra trong quá trình chèn dữ liệu vào cơ sở dữ liệu: " + ex.Message, ex);
            }
        }


        // lấy 1 danh mục
        public async Task<Categorys> GetByIdAsync(string id)
        {
            try
            {
                var objectId = ObjectId.Parse(id);
                var filter = Builders<Categorys>.Filter.Eq("_id", objectId);

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
        //public async Task<bool> UpdateAsync(string id, ProjectFunds updatedEntity)
        //{
        //    if (string.IsNullOrEmpty(id) || updatedEntity == null)
        //    {
        //        throw new ArgumentException("Invalid id or entity.");
        //    }

        //    // Tạo filter để tìm tài liệu cần cập nhật theo _id
        //    var filter = Builders<ProjectFunds>.Filter.Eq("_id", ObjectId.Parse(id));

        //    // Tìm tài liệu trước khi cập nhật để lấy ảnh cũ
        //    var existingEntity = await _collection.Find(filter).FirstOrDefaultAsync();

        //    // Danh sách các cập nhật
        //    var updates = new List<UpdateDefinition<ProjectFunds>>();

        //    // Tạo một phương thức để thêm các trường vào danh sách cập nhật chỉ khi chúng khác null hoặc không rỗng
        //    void AddUpdate(Expression<Func<ProjectFunds, object>> field, object value)
        //    {
        //        if (value != null && !string.IsNullOrEmpty(value.ToString())) // Kiểm tra null hoặc chuỗi rỗng
        //        {
        //            updates.Add(Builders<ProjectFunds>.Update.Set(field, value));
        //        }
        //    }

        //    // Thêm các trường vào danh sách cập nhật
        //    AddUpdate(x => x.name, updatedEntity.name);
        //    AddUpdate(x => x.idFund, updatedEntity.idFund);
        //    AddUpdate(x => x.nameFund, updatedEntity.nameFund);
        //    AddUpdate(x => x.description, updatedEntity.description);
        //    AddUpdate(x => x.targetAmount, updatedEntity.targetAmount);
        //    AddUpdate(x => x.currentAmount, updatedEntity.currentAmount);
        //    AddUpdate(x => x.startDate, updatedEntity.startDate);
        //    AddUpdate(x => x.endDate, updatedEntity.endDate);

        //    // Xử lý ảnh mới nếu có
        //    if (updatedEntity.imagesIFormFile != null)
        //    {
        //        // Lưu ảnh mới và lấy đường dẫn của ảnh
        //        var newImageFilePath = await SaveFileHelper.SaveFileAsync(updatedEntity.imagesIFormFile);

        //        // Nếu có ảnh cũ trong cơ sở dữ liệu và có ảnh mới, xóa ảnh cũ
        //        if (existingEntity != null && !string.IsNullOrEmpty(existingEntity.images))
        //        {
        //            // Xóa ảnh cũ
        //            SaveFileHelper.DeleteProjectFile(existingEntity.images);
        //        }

        //        // Cập nhật file ảnh mới
        //        updates.Add(Builders<ProjectFunds>.Update.Set(x => x.images, newImageFilePath));
        //    }

        //    // Kết hợp tất cả các cập nhật thành một UpdateDefinition
        //    var updateDefinition = Builders<ProjectFunds>.Update.Combine(updates);

        //    // Thực hiện cập nhật tài liệu
        //    var result = await _collection.UpdateOneAsync(filter, updateDefinition);

        //    // Kiểm tra xem có tài liệu nào được cập nhật không
        //    return result.MatchedCount > 0;
        //}
        public async Task<bool> UpdateAsync(string id, Categorys updatedEntity)
        {
            if (string.IsNullOrEmpty(id) || updatedEntity == null)
            {
                throw new ArgumentException("Invalid id or entity.");
            }

            // Tạo filter để tìm tài liệu cần cập nhật theo _id
            var filter = Builders<Categorys>.Filter.Eq("_id", ObjectId.Parse(id));

            // Tìm tài liệu trước khi cập nhật để lấy ảnh cũ
            var existingEntity = await _collection.Find(filter).FirstOrDefaultAsync();

            // Danh sách các cập nhật
            var updates = new List<UpdateDefinition<Categorys>>();

            // Tạo một phương thức để thêm các trường vào danh sách cập nhật chỉ khi chúng khác null hoặc không rỗng
            void AddUpdate(Expression<Func<Categorys, object>> field, object value)
            {
                if (value != null && !string.IsNullOrEmpty(value.ToString())) // Kiểm tra null hoặc chuỗi rỗng
                {
                    updates.Add(Builders<Categorys>.Update.Set(field, value));
                }
            }

            // Thêm các trường vào danh sách cập nhật
            AddUpdate(x => x.name, updatedEntity.name);
            AddUpdate(x => x.updatedAt, DateTime.UtcNow);


            // Kết hợp tất cả các cập nhật thành một UpdateDefinition
            var updateDefinition = Builders<Categorys>.Update.Combine(updates);

            // Thực hiện cập nhật tài liệu
            var result = await _collection.UpdateOneAsync(filter, updateDefinition);

            // Kiểm tra xem có tài liệu nào được cập nhật không
            return result.MatchedCount > 0;
        }


        // lấy list các dự án
        public async Task<List<Categorys>> GetAllAsync(int skipAmount, int pageSize, string searchValue = null)
        {
            var sortDefinition = Builders<Categorys>.Sort.Descending(x => x.Id);

            // Xây dựng điều kiện truy vấn cơ bản
            var filterDefinition = Builders<Categorys>.Filter.Empty;

            // Nếu có searchValue, thêm bộ lọc tìm kiếm
            if (!string.IsNullOrEmpty(searchValue))
            {
                // Loại bỏ dấu và chuyển thành chữ thường

                // Tạo bộ lọc tìm kiếm cho trường name không phân biệt hoa thường
                var searchFilter = Builders<Categorys>.Filter.Regex(
                    x => x.name, new BsonRegularExpression($"(?i).*{searchValue}.*")
                );

                filterDefinition &= searchFilter; // Áp dụng bộ lọc tìm kiếm vào filter
            }

            // Lấy danh sách các Categorys với điều kiện lọc, phân trang và sắp xếp
            var categorys = await _collection.Find(filterDefinition)
                                            .Skip(skipAmount)
                                            .Sort(sortDefinition)
                                            .Limit(pageSize)
                                            .ToListAsync();

            return categorys;
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
                    objectIdList.Add(objectId);
                }
                else
                {
                    throw new ArgumentException($"Invalid id format: {id}");
                }
            }

            // Tạo filter để xóa tất cả các quỹ theo danh sách ObjectId
            var filter = Builders<Categorys>.Filter.In("_id", objectIdList);

            // Thực hiện xóa các quỹ
            var result = await _collection.DeleteManyAsync(filter);

            return result.DeletedCount;
        }

        //public async Task<long> DeleteByIdsAsync(List<string> ids)
        //{
        //    if (ids == null || ids.Count == 0)
        //    {
        //        throw new ArgumentException("The list of ids cannot be null or empty.");
        //    }

        //    var objectIdList = new List<ObjectId>();

        //    foreach (var id in ids)
        //    {
        //        if (ObjectId.TryParse(id, out var objectId))
        //        {
        //            // Tạo filter để tìm tài liệu cần cập nhật theo _id
        //            var filterFund = Builders<ProjectFunds>.Filter.Eq("_id", ObjectId.Parse(id));

        //            // Tìm tài liệu trước khi cập nhật để lấy ảnh cũ
        //            var existingEntity = await _collection.Find(filterFund).FirstOrDefaultAsync();
        //            // Nếu có ảnh cũ trong cơ sở dữ liệu và có ảnh mới, xóa ảnh cũ
        //            if (existingEntity != null && !string.IsNullOrEmpty(existingEntity.images))
        //            {
        //                // Xóa ảnh cũ
        //                SaveFileHelper.DeleteProjectFile(existingEntity.images);
        //            }

        //            objectIdList.Add(objectId);
        //        }
        //        else
        //        {
        //            throw new ArgumentException($"Invalid id format: {id}");
        //        }
        //    }

        //    var filter = Builders<ProjectFunds>.Filter.In("_id", objectIdList);
        //    var result = await _collection.DeleteManyAsync(filter);

        //    return result.DeletedCount;
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

