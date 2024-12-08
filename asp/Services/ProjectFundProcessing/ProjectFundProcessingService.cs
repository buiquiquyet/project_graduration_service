using asp.Constants;
using asp.Constants.ProjectFundConst;
using asp.Helper.ConnectDb;
using asp.Helper.File;
using asp.Models;
using asp.Models.ProjectFundProcessing;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using System.Net;
using System.Numerics;
using System.Text.RegularExpressions;

namespace asp.Services.ProjectFundDone
{


    public class ProjectFundProcessingService
    {
        private readonly IMongoCollection<ProjectFundsProcessing> _collection;
        private readonly IMongoCollection<CharityFunds> _collectionCharityFund;
        private readonly IMongoCollection<Categorys> _collectionCategory;
        private readonly IMongoCollection<ProjectFunds> _collectionProjectFunds;
        private readonly IMongoCollection<MomoExecuteResponseModel> _collectionMomoCreatePaymentResponseModel;

        public ProjectFundProcessingService(ConnectDbHelper dbHelper)
        {
            _collection = dbHelper.GetCollection<ProjectFundsProcessing>();
            _collectionCharityFund = dbHelper.GetCollection<CharityFunds>();
            _collectionCategory = dbHelper.GetCollection<Categorys>();
            _collectionProjectFunds = dbHelper.GetCollection<ProjectFunds>();
            _collectionMomoCreatePaymentResponseModel = dbHelper.GetCollection<MomoExecuteResponseModel>();
        }
        //tạo dự án
        public async Task<ProjectFundsProcessing> Create(ProjectFundsProcessing request)
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

            // Tạo đối tượng ProjectFundsProcessing từ dữ liệu request
            var registerAuth = new ProjectFundsProcessing
            {
                idFund = request.idFund,
                idCategory = request.idCategory,
                name = request.name,
                images = imageFileNames, // Gán danh sách tên file ảnh
                description = request.description,
                targetAmount = request.targetAmount,
                currentAmount = request.currentAmount,
                isApproved = ApprovedConst.PROCESSING,
                userId = request.userId,
                userName = request.userName,
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

        public async Task<ProjectFundsProcessing> GetByIdAsync(string id)
        {
            try
            {
                var objectId = ObjectId.Parse(id);
                var filter = Builders<ProjectFundsProcessing>.Filter.Eq("_id", objectId);

                // Truy vấn ProjectFunds bằng id
                var projectFunds = await _collection.Find(filter).FirstOrDefaultAsync();

                if (projectFunds == null)
                {
                    return null;
                }

                // Truy vấn charityFund bằng idFund của ProjectFunds
                CharityFunds charityFund = null;
                if (projectFunds.idFund != null)
                {
                    charityFund = await _collectionCharityFund
                        .Find(fund => fund.Id == projectFunds.idFund)
                        .FirstOrDefaultAsync();
                }

                // Truy vấn category bằng idCategory của ProjectFunds
                Categorys category = null;
                if (projectFunds.idCategory != null)
                {
                    category = await _collectionCategory
                        .Find(cat => cat.Id == projectFunds.idCategory)
                        .FirstOrDefaultAsync();
                }

                // Gán tên và hình ảnh của charityFund và category vào ProjectFunds
                if (charityFund != null)
                {
                    projectFunds.nameFund = charityFund.name;
                    projectFunds.imageFund = charityFund.images;
                }
                // lấy tên danh mục
                if (category != null)
                {
                    projectFunds.nameCategory = category.name;
                }
                // Tính phần trăm đạt được
                if (!string.IsNullOrEmpty(projectFunds.currentAmount) &&
                    decimal.TryParse(projectFunds.currentAmount, out var currentAmount) &&
                    decimal.TryParse(projectFunds.targetAmount, out var targetAmount) &&
                    targetAmount > 0)
                {
                    projectFunds.percent = (currentAmount / targetAmount * 100).ToString("0.##");
                }
                else
                {
                    projectFunds.percent = "0";
                }
                // Đếm số lượng bản ghi trong MomoCreatePaymentResponseModel với projectFundId tương ứng
                var paymentFilter = Builders<MomoExecuteResponseModel>.Filter.Eq("ProjectFundId", projectFunds.Id);
                projectFunds.numberOfDonate = await _collectionMomoCreatePaymentResponseModel.CountDocumentsAsync(paymentFilter);

                return projectFunds;
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Format exception: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving project fund: {ex.Message}");
                return null;
            }
        }
        // hàm update trạng thái dự án sứ giả
        public async Task<bool> UpdateApprovalStatusAsync(UpdateApprovalStatusDTO dto)
        {
            if (dto.Ids == null || !dto.Ids.Any() || string.IsNullOrEmpty(dto.isApproved))
            {
                throw new ArgumentException("Ids and IsApproved are required.");
            }

            var filter = Builders<ProjectFundsProcessing>.Filter.In(p => p.Id, dto.Ids);
            var update = Builders<ProjectFundsProcessing>.Update.Set(p => p.isApproved, dto.isApproved)
                                                               .Set(p => p.updatedAt, DateTime.Now);
            var result = await _collection.UpdateManyAsync(filter, update);

            if (result.ModifiedCount > 0)
            {
                foreach (var id in dto.Ids)
                {
                    var project = await _collection.Find(p => p.Id == id).FirstOrDefaultAsync();

                    if (project != null)
                    {
                        if (dto.isApproved == ApprovedConst.APPROVED)
                        {
                            var newRecord = new ProjectFunds
                            {
                                idFund = project.idFund,
                                idCategory = project.idCategory,
                                name = project.name,
                                images = project.images,
                                description = project.description,
                                targetAmount = project.targetAmount,
                                currentAmount = project.currentAmount,
                                startDate = project.startDate,
                                idProjectFundProcessing = project.Id,
                                endDate = project.endDate,
                                createdAt = DateTime.UtcNow,
                                updatedAt = DateTime.UtcNow,
                                userId = project.userId,
                                userName = project.userName,
                            };

                            try
                            {
                                await _collectionProjectFunds.InsertOneAsync(newRecord);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("Có lỗi xảy ra trong quá trình chèn dữ liệu vào cơ sở dữ liệu: " + ex.Message, ex);
                            }
                        }
                        else if (dto.isApproved == ApprovedConst.PROCESSING || dto.isApproved == ApprovedConst.REJECTED)
                        {
                            // Check if there is a matching record in ProjectFunds
                            var projectFundsFilter = Builders<ProjectFunds>.Filter.Eq(p => p.idProjectFundProcessing, project.Id);
                            var existingProjectFund = await _collectionProjectFunds.Find(projectFundsFilter).FirstOrDefaultAsync();

                            if (existingProjectFund != null)
                            {
                                var deleteResult = await _collectionProjectFunds.DeleteOneAsync(projectFundsFilter);

                                if (!deleteResult.IsAcknowledged)
                                {
                                    throw new Exception("Có lỗi xảy ra trong quá trình xóa dữ liệu từ cơ sở dữ liệu.");
                                }
                            }
                        }
                    }
                }
            }

            return result.ModifiedCount > 0;
        }

        //public async Task<bool> UpdateApprovalStatusAsync(UpdateApprovalStatusDTO dto)
        //{
        //    if (dto.Ids == null || !dto.Ids.Any() || string.IsNullOrEmpty(dto.isApproved))
        //    {
        //        throw new ArgumentException("Ids and IsApproved are required.");
        //    }

        //    var filter = Builders<ProjectFundsProcessing>.Filter.In(p => p.Id, dto.Ids);
        //    var update = Builders<ProjectFundsProcessing>.Update.Set(p => p.isApproved, dto.isApproved)
        //                                                       .Set(p => p.updatedAt, DateTime.Now);
        //    var result = await _collection.UpdateManyAsync(filter, update);

        //    // Nếu isApproved là "approved" và có ít nhất một bản ghi được cập nhật, tạo bản ghi mới
        //    if (dto.isApproved == ApprovedConst.APPROVED && result.ModifiedCount > 0)
        //    {
        //        foreach (var id in dto.Ids)
        //        {
        //            var project = await _collection.Find(p => p.Id == id).FirstOrDefaultAsync();
        //            if (project != null)
        //            {
        //                var newRecord = new ProjectFunds
        //                {
        //                    idFund = project.idFund,
        //                    idCategory = project.idCategory,
        //                    name = project.name,
        //                    images = project.images, // hoặc logic xử lý ảnh tương tự hàm Create
        //                    description = project.description,
        //                    targetAmount = project.targetAmount,
        //                    currentAmount = project.currentAmount,
        //                    startDate = project.startDate,
        //                    endDate = project.endDate,
        //                    createdAt = DateTime.UtcNow,
        //                    updatedAt = DateTime.UtcNow,
        //                };

        //                try
        //                {
        //                    // Chèn dữ liệu vào collection MongoDB
        //                    await _collectionProjectFunds.InsertOneAsync(newRecord);
        //                }
        //                catch (Exception ex)
        //                {
        //                    throw new Exception("Có lỗi xảy ra trong quá trình chèn dữ liệu vào cơ sở dữ liệu: " + ex.Message, ex);
        //                }
        //            }
        //        }
        //    }

        //    return result.ModifiedCount > 0;
        //}

        //public async Task<bool> UpdateApprovalStatusAsync(UpdateApprovalStatusDTO dto)
        //{
        //    if (dto.Ids == null || !dto.Ids.Any() || string.IsNullOrEmpty(dto.isApproved))
        //    {
        //        throw new ArgumentException("Ids and IsApproved are required.");
        //    }

        //    var filter = Builders<ProjectFundsProcessing>.Filter.In(p => p.Id, dto.Ids);
        //    var update = Builders<ProjectFundsProcessing>.Update.Set(p => p.isApproved, dto.isApproved)
        //                                                       .Set(p => p.updatedAt, DateTime.Now);
        //    var result = await _collection.UpdateManyAsync(filter, update);
        //    return result.ModifiedCount > 0;
        //}
        // hàm update thông tin dự án
        public async Task<bool> UpdateAsync(string id, ProjectFundsProcessing updatedEntity)
        {
            if (string.IsNullOrEmpty(id) || updatedEntity == null)
            {
                throw new ArgumentException("Invalid id or entity.");
            }

            // Tạo filter để tìm tài liệu cần cập nhật theo _id
            var filter = Builders<ProjectFundsProcessing>.Filter.Eq("_id", ObjectId.Parse(id));

            // Tìm tài liệu trước khi cập nhật để lấy ảnh cũ
            var existingEntity = await _collection.Find(filter).FirstOrDefaultAsync();

            // Danh sách các cập nhật
            var updates = new List<UpdateDefinition<ProjectFundsProcessing>>();

            // Tạo một phương thức để thêm các trường vào danh sách cập nhật chỉ khi chúng khác null hoặc không rỗng
            void AddUpdate(Expression<Func<ProjectFundsProcessing, object>> field, object value)
            {
                if (value != null && !string.IsNullOrEmpty(value.ToString())) // Kiểm tra null hoặc chuỗi rỗng
                {
                    updates.Add(Builders<ProjectFundsProcessing>.Update.Set(field, value));
                }
            }

            // Thêm các trường vào danh sách cập nhật
            AddUpdate(x => x.name, updatedEntity.name);
            AddUpdate(x => x.idFund, updatedEntity.idFund);
            AddUpdate(x => x.idCategory, updatedEntity.idCategory);
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
                updates.Add(Builders<ProjectFundsProcessing>.Update.Set(x => x.images, newImageFilePaths));
            }

            // Kết hợp tất cả các cập nhật thành một UpdateDefinition
            var updateDefinition = Builders<ProjectFundsProcessing>.Update.Combine(updates);

            // Thực hiện cập nhật tài liệu
            var result = await _collection.UpdateOneAsync(filter, updateDefinition);

            // Kiểm tra xem có tài liệu nào được cập nhật không
            return result.MatchedCount > 0;
        }


        // lấy list các dự án
        public async Task<List<ProjectFundsProcessing>> GetAllAsync(int skipAmount, int pageSize, string filterType = ApprovedConst.PROCESSING, string userId = "")
        {
            var sortDefinition = Builders<ProjectFundsProcessing>.Sort.Descending(x => x.Id);

            // Xây dựng điều kiện truy vấn cơ bản
            var filterDefinition = Builders<ProjectFundsProcessing>.Filter.Empty;

            // Nếu userId không null hoặc rỗng, thêm điều kiện tìm kiếm theo userId
            if (!string.IsNullOrEmpty(userId))
            {
                filterDefinition &= Builders<ProjectFundsProcessing>.Filter.Eq(p => p.userId, userId);
            }

            // Lấy tất cả ProjectFunds với các trang (skip và limit)
            var projectFunds = await _collection.Find(filterDefinition)
                                                .Skip(skipAmount)
                                                .Sort(sortDefinition)
                                                .Limit(pageSize)
                                                .ToListAsync();

            // Lấy danh sách các idFund duy nhất từ ProjectFunds
            var fundIds = projectFunds.Select(p => p.idFund).Distinct().ToList();
            // Lấy danh sách các idCategory duy nhất từ ProjectFunds
            var categoryIds = projectFunds.Select(p => p.idCategory).Distinct().ToList();

            // Truy vấn CharityFunds theo danh sách fundIds
            var charityFunds = await _collectionCharityFund
                .Find(fund => fundIds.Contains(fund.Id))
                .ToListAsync();
            // Truy vấn Categorys theo danh sách categoryIds
            var categorys = await _collectionCategory
                .Find(fund => categoryIds.Contains(fund.Id))
                .ToListAsync();

            // Tạo một dictionary ánh xạ idFund -> name từ CharityFunds
            var fundNamesMapping = charityFunds.ToDictionary(fund => fund.Id, fund => new { fund.name, fund.images });
            // Tạo một dictionary ánh xạ idFund -> name từ Categorys
            var categoryNamesMapping = categorys.ToDictionary(category => category.Id, category => category.name);

            // Gán tên của CharityFund vào ProjectFunds
            foreach (var project in projectFunds)
            {
                // Kiểm tra xem idFund có phải là null không
                if (project.idFund != null && fundNamesMapping.ContainsKey(project.idFund))
                {
                    var fund = fundNamesMapping[project.idFund];
                    project.nameFund = fund.name; // Gán tên vào thuộc tính nameFund
                    project.imageFund = fund.images; // Gán danh sách ảnh vào thuộc tính imageFund
                }
                // Kiểm tra xem idFund có phải là null không
                if (project.idCategory != null && categoryNamesMapping.ContainsKey(project.idCategory))
                {
                    project.nameCategory = categoryNamesMapping[project.idCategory]; // Gán tên vào thuộc tính nameCategory
                }
                else
                {
                    // Nếu không có idFund hợp lệ, bạn có thể gán giá trị mặc định hoặc bỏ qua
                    project.nameFund = "Unknown Fund"; // Hoặc bạn có thể để trống (null)
                }
                // Tính phần trăm đạt được
                if (!string.IsNullOrEmpty(project.currentAmount) &&
                    decimal.TryParse(project.currentAmount, out var currentAmount) &&
                    decimal.TryParse(project.targetAmount, out var targetAmount) &&
                    targetAmount > 0)
                {
                    project.percent = (currentAmount / targetAmount * 100).ToString("0.##");
                }
                else
                {
                    project.percent = "0";
                }
            }

            // Lọc các dự án theo endDate
            var currentDate = DateTime.Now;

            // Lọc các dự án dựa trên isApproved và endDate
            if (filterType == ApprovedConst.PROCESSING)
            {
                // Lọc các dự án có isApproved là PROCESSING
                projectFunds = projectFunds
                    .Where(p => p.isApproved == ApprovedConst.PROCESSING)
                    .ToList();
            }
            else if (filterType == ApprovedConst.APPROVED)
            {
                // Lọc các dự án có isApproved là APPROVED
                projectFunds = projectFunds
                    .Where(p => p.isApproved == ApprovedConst.APPROVED)
                    .ToList();
            }
            else if (filterType == ApprovedConst.REJECTED)
            {
                // Lọc các dự án có isApproved là REJECTED
                projectFunds = projectFunds
                    .Where(p => p.isApproved == ApprovedConst.REJECTED)
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
                    var filterFund = Builders<ProjectFundsProcessing>.Filter.Eq("_id", ObjectId.Parse(id));

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
            var filter = Builders<ProjectFundsProcessing>.Filter.In("_id", objectIdList);

            // Thực hiện xóa các quỹ
            var result = await _collection.DeleteManyAsync(filter);

            return result.DeletedCount;
        }

       

    }
}

