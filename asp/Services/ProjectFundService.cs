﻿using asp.Constants;
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


    public class ProjectFundService
    {
        private readonly IMongoCollection<ProjectFunds> _collection;
        private readonly IMongoCollection<CharityFunds> _collectionCharityFund;

        public ProjectFundService(ConnectDbHelper dbHelper)
        {
            _collection = dbHelper.GetCollection<ProjectFunds>();
            _collectionCharityFund = dbHelper.GetCollection<CharityFunds>();
        }
        //tạo dự án
        public async Task<ProjectFunds> Create(ProjectFunds request)
        {
            // Biến lưu danh sách tên file ảnh
            var imageFileNames = new List<string>();

            // Kiểm tra và xử lý file ảnh
            if (request.imagesIFormFile != null && request.imagesIFormFile.Count > 0)
            {
                foreach (var imageFile in request.imagesIFormFile)
                {
                    // Lưu từng file và lấy tên file
                    var fileName = await SaveFileHelper.SaveFileAsync(imageFile);
                    imageFileNames.Add(fileName);
                }
            }
            else
            {
                // Trường hợp không có ảnh (nếu cần thiết có thể gán giá trị mặc định)
                imageFileNames = new List<string>();
            }

            // Tạo đối tượng ProjectFunds từ dữ liệu request
            var registerAuth = new ProjectFunds
            {
                idFund = request.idFund,
                idCategory = request.idCategory,
                name = request.name,
                images = imageFileNames, // Gán danh sách tên file ảnh
                description = request.description,
                targetAmount = request.targetAmount,
                currentAmount = request.currentAmount,
                startDate = request.startDate,
                endDate = request.endDate,
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


        // lấy 1 dự án
        public async Task<ProjectFunds> GetByIdAsync(string id)
        {
            try
            {
                var objectId = ObjectId.Parse(id);
                var filter = Builders<ProjectFunds>.Filter.Eq("_id", objectId);

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
        public async Task<bool> UpdateAsync(string id, ProjectFunds updatedEntity)
        {
            if (string.IsNullOrEmpty(id) || updatedEntity == null)
            {
                throw new ArgumentException("Invalid id or entity.");
            }

            // Tạo filter để tìm tài liệu cần cập nhật theo _id
            var filter = Builders<ProjectFunds>.Filter.Eq("_id", ObjectId.Parse(id));

            // Tìm tài liệu trước khi cập nhật để lấy ảnh cũ
            var existingEntity = await _collection.Find(filter).FirstOrDefaultAsync();

            // Danh sách các cập nhật
            var updates = new List<UpdateDefinition<ProjectFunds>>();

            // Tạo một phương thức để thêm các trường vào danh sách cập nhật chỉ khi chúng khác null hoặc không rỗng
            void AddUpdate(Expression<Func<ProjectFunds, object>> field, object value)
            {
                if (value != null && !string.IsNullOrEmpty(value.ToString())) // Kiểm tra null hoặc chuỗi rỗng
                {
                    updates.Add(Builders<ProjectFunds>.Update.Set(field, value));
                }
            }

            // Thêm các trường vào danh sách cập nhật
            AddUpdate(x => x.name, updatedEntity.name);
            AddUpdate(x => x.idFund, updatedEntity.idFund);
            AddUpdate(x => x.nameFund, updatedEntity.nameFund);
            AddUpdate(x => x.description, updatedEntity.description);
            AddUpdate(x => x.targetAmount, updatedEntity.targetAmount);
            AddUpdate(x => x.currentAmount, updatedEntity.currentAmount);
            AddUpdate(x => x.startDate, updatedEntity.startDate);
            AddUpdate(x => x.endDate, updatedEntity.endDate);
            AddUpdate(x => x.updatedAt, DateTime.UtcNow);

            // Xử lý ảnh mới nếu có
            if (updatedEntity.imagesIFormFile != null && updatedEntity.imagesIFormFile.Count > 0)
            {
                // Lưu tất cả các ảnh mới và lấy đường dẫn của các ảnh
                var newImageFilePaths = new List<string>();
                foreach (var imageFile in updatedEntity.imagesIFormFile)
                {
                    var newImageFilePath = await SaveFileHelper.SaveFileAsync(imageFile);
                    newImageFilePaths.Add(newImageFilePath);
                }

                // Nếu có ảnh cũ trong cơ sở dữ liệu và có ảnh mới, xóa ảnh cũ
                if (existingEntity != null && existingEntity.images != null && existingEntity.images.Count > 0)
                {
                    foreach (var oldImage in existingEntity.images)
                    {
                        // Xóa ảnh cũ
                        SaveFileHelper.DeleteProjectFile(oldImage);
                    }
                }

                // Cập nhật file ảnh mới
                updates.Add(Builders<ProjectFunds>.Update.Set(x => x.images, newImageFilePaths));
            }

            // Kết hợp tất cả các cập nhật thành một UpdateDefinition
            var updateDefinition = Builders<ProjectFunds>.Update.Combine(updates);

            // Thực hiện cập nhật tài liệu
            var result = await _collection.UpdateOneAsync(filter, updateDefinition);

            // Kiểm tra xem có tài liệu nào được cập nhật không
            return result.MatchedCount > 0;
        }


        // lấy list các dự án
        public async Task<List<ProjectFunds>> GetAllAsync(int skipAmount, int pageSize, string filterType = FilterListProjectFund.ALL)
        {
            var sortDefinition = Builders<ProjectFunds>.Sort.Descending(x => x.Id);

            // Lấy tất cả ProjectFunds với các trang (skip và limit)
            var projectFunds = await _collection.Find(_ => true)
                                                .Skip(skipAmount)
                                                .Sort(sortDefinition)
                                                .Limit(pageSize)
                                                .ToListAsync();

            // Lấy danh sách các idFund duy nhất từ ProjectFunds
            var fundIds = projectFunds.Select(p => p.idFund).Distinct().ToList();

            // Truy vấn CharityFunds theo danh sách fundIds
            var charityFunds = await _collectionCharityFund
                .Find(fund => fundIds.Contains(fund.Id))
                .ToListAsync();

            // Tạo một dictionary ánh xạ idFund -> name từ CharityFunds
            var fundNamesMapping = charityFunds.ToDictionary(fund => fund.Id, fund => fund.name);

            // Gán tên của CharityFund vào ProjectFunds
            foreach (var project in projectFunds)
            {
                // Kiểm tra xem idFund có phải là null không
                if (project.idFund != null && fundNamesMapping.ContainsKey(project.idFund))
                {
                    project.nameFund = fundNamesMapping[project.idFund]; // Gán tên vào thuộc tính nameFund
                }
                else
                {
                    // Nếu không có idFund hợp lệ, bạn có thể gán giá trị mặc định hoặc bỏ qua
                    project.nameFund = "Unknown Fund"; // Hoặc bạn có thể để trống (null)
                }
            }

            // Lọc các dự án theo endDate
            var currentDate = DateTime.Now;

            if (filterType == FilterListProjectFund.ENDED)
            {
                // Lọc các dự án có endDate trước ngày hôm nay
                projectFunds = projectFunds
                    .Where(p => !string.IsNullOrEmpty(p.endDate) && DateTime.TryParse(p.endDate, out DateTime endDateValue) && endDateValue.Date < currentDate.Date)
                    .ToList();
            }
            else if (filterType == FilterListProjectFund.IN_PROCESSING)
            {
                // Lọc các dự án có endDate sau ngày hôm nay
                projectFunds = projectFunds
                    .Where(p => !string.IsNullOrEmpty(p.endDate) && DateTime.TryParse(p.endDate, out DateTime endDateValue) && endDateValue.Date >= currentDate.Date)
                    .ToList();
            }

            // Nếu không có filterType, lấy tất cả các dự án mà không lọc theo endDate
            return projectFunds;
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
                    // Tạo filter để tìm tài liệu cần xóa theo _id
                    var filterFund = Builders<ProjectFunds>.Filter.Eq("_id", ObjectId.Parse(id));

                    // Tìm tài liệu trước khi xóa để lấy ảnh cũ
                    var existingEntity = await _collection.Find(filterFund).FirstOrDefaultAsync();

                    // Nếu tài liệu tồn tại và có ảnh, xóa tất cả ảnh cũ
                    if (existingEntity != null && existingEntity.images != null && existingEntity.images.Count > 0)
                    {
                        // Lặp qua danh sách ảnh và xóa từng ảnh
                        foreach (var image in existingEntity.images)
                        {
                            // Xóa từng ảnh
                            SaveFileHelper.DeleteProjectFile(image);
                        }
                    }

                    objectIdList.Add(objectId);
                }
                else
                {
                    throw new ArgumentException($"Invalid id format: {id}");
                }
            }

            // Tạo filter để xóa tất cả các quỹ theo danh sách ObjectId
            var filter = Builders<ProjectFunds>.Filter.In("_id", objectIdList);

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

