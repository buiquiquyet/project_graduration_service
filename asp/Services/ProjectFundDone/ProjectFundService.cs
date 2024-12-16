using asp.Constants;
using asp.Constants.ProjectFundConst;
using asp.Helper;
using asp.Helper.ConnectDb;
using asp.Helper.File;
using asp.Models;
using asp.Models.ProjectFund;
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
        private readonly IMongoCollection<Categorys> _collectionCategory; 
        private readonly IMongoCollection<MomoExecuteResponseModel> _collectionMomoCreatePaymentResponseModel; 

        public ProjectFundService(ConnectDbHelper dbHelper)
        {
            _collection = dbHelper.GetCollection<ProjectFunds>();
            _collectionCharityFund = dbHelper.GetCollection<CharityFunds>();
            _collectionCategory = dbHelper.GetCollection<Categorys>();
            _collectionMomoCreatePaymentResponseModel = dbHelper.GetCollection<MomoExecuteResponseModel>();
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

            // lưu file video
            var videoFileNames = new List<string>();
            // Kiểm tra và xử lý file video
            if (request.videoIFormFile != null && request.videoIFormFile.Count > 0)
            {
                foreach (var videoFile in request.videoIFormFile)
                {
                    // Lưu từng file và lấy tên file
                    var fileName = await SaveFileHelper.SaveFileAsync(videoFile);
                    videoFileNames.Add(fileName);
                }
            }
            else
            {
                // Trường hợp không có ảnh (nếu cần thiết có thể gán giá trị mặc định)
                videoFileNames = new List<string>();
            }

            // Tạo đối tượng ProjectFunds từ dữ liệu request
            var registerAuth = new ProjectFunds
            {
                idFund = request.idFund,
                idCategory = request.idCategory,
                name = request.name,
                images = imageFileNames, // Gán danh sách tên file ảnh
                video = videoFileNames, // Gán danh sách tên file video
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
                    projectFunds.percent = ((currentAmount / targetAmount) * 100).ToString("0.##");
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

        // hàm update thông tin dự án
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
                updates.Add(Builders<ProjectFunds>.Update.Set(x => x.images, newImageFilePaths));
            }
            // Xử lý video mới nếu có
            if (updatedEntity.videoIFormFile != null && updatedEntity.videoIFormFile.Count > 0)
            {
                // Lưu tất cả các ảnh mới và lấy đường dẫn của các video
                var newVideoFilePaths = new List<string>();
                foreach (var videoFile in updatedEntity.videoIFormFile)
                {
                    var newImageFilePath = await SaveFileHelper.SaveFileAsync(videoFile);
                    newVideoFilePaths.Add(newImageFilePath);
                }

                // Nếu có video cũ trong cơ sở dữ liệu và có video mới, xóa video cũ
                if (existingEntity != null && existingEntity.video != null && existingEntity.video.Count > 0)
                {
                    foreach (var oldVideo in existingEntity.video)
                    {
                        // Xóa video cũ
                        SaveFileHelper.DeleteProjectFile(oldVideo);
                    }
                }

                // Cập nhật file video mới
                updates.Add(Builders<ProjectFunds>.Update.Set(x => x.video, newVideoFilePaths));
            }

            // Kết hợp tất cả các cập nhật thành một UpdateDefinition
            var updateDefinition = Builders<ProjectFunds>.Update.Combine(updates);

            // Thực hiện cập nhật tài liệu
            var result = await _collection.UpdateOneAsync(filter, updateDefinition);

            // Kiểm tra xem có tài liệu nào được cập nhật không
            return result.MatchedCount > 0;
        }
        // lấy list của các dự án
        public async Task<List<ProjectFunds>> GetAllAsync(int skipAmount, int pageSize, string filterType = FilterListProjectFund.ALL, string fundId = null, string searchValue = null)
        {
            var sortDefinition = Builders<ProjectFunds>.Sort.Descending(x => x.Id);

            // Điều chỉnh filter để bao gồm fundId nếu được truyền lên
            var filter = Builders<ProjectFunds>.Filter.Empty;
            if (!string.IsNullOrEmpty(fundId))
            {
                filter = Builders<ProjectFunds>.Filter.Eq(x => x.idFund, fundId);
            }

            // Điều chỉnh filter để bao gồm điều kiện tìm kiếm nếu searchTerm không rỗng hoặc null
            if (!string.IsNullOrEmpty(searchValue))
            {
                var normalizedSearchValue = SearchVie.RemoveDiacritics(searchValue);

                // Tìm kiếm trên các trường có sẵn trong tài liệu gốc
                var searchFilter = Builders<ProjectFunds>.Filter.Or(
                    Builders<ProjectFunds>.Filter.Regex(x => x.name, new BsonRegularExpression($".*{searchValue}.*", "i")),
                    Builders<ProjectFunds>.Filter.Regex(x => x.name, new BsonRegularExpression($".*{normalizedSearchValue}.*", "i"))
                );

                // Tìm kiếm nameFund trong bộ sưu tập CharityFunds trước
                var charityFundFilter = Builders<CharityFunds>.Filter.Or(
                    Builders<CharityFunds>.Filter.Regex(x => x.name, new BsonRegularExpression($".*{searchValue}.*", "i")),
                    Builders<CharityFunds>.Filter.Regex(x => x.name, new BsonRegularExpression($".*{normalizedSearchValue}.*", "i"))
                );
                var charityFundsSearch = await _collectionCharityFund.Find(charityFundFilter).ToListAsync();
                var fundIds = charityFundsSearch.Select(f => f.Id).ToList();

                if (fundIds.Any())
                {
                    var fundIdFilter = Builders<ProjectFunds>.Filter.In(x => x.idFund, fundIds);
                    searchFilter = Builders<ProjectFunds>.Filter.Or(searchFilter, fundIdFilter);
                }

                filter = Builders<ProjectFunds>.Filter.And(filter, searchFilter);
            }

            // Lấy tất cả ProjectFunds với các trang (skip và limit)
            var projectFunds = await _collection.Find(filter)
                                                .Skip(skipAmount)
                                                .Sort(sortDefinition)
                                                .Limit(pageSize)
                                                .ToListAsync();

            // Lấy danh sách các idFund duy nhất từ ProjectFunds
            var uniqueFundIds = projectFunds.Select(p => p.idFund).Distinct().ToList();
            // Lấy danh sách các idCategory duy nhất từ ProjectFunds
            var uniqueCategoryIds = projectFunds.Select(p => p.idCategory).Distinct().ToList();

            // Truy vấn CharityFunds theo danh sách uniqueFundIds
            var charityFunds = await _collectionCharityFund
                .Find(fund => uniqueFundIds.Contains(fund.Id))
                .ToListAsync();
            // Truy vấn Categorys theo danh sách uniqueCategoryIds
            var categorys = await _collectionCategory
                .Find(category => uniqueCategoryIds.Contains(category.Id))
                .ToListAsync();

            // Tạo một dictionary ánh xạ idFund -> name từ CharityFunds
            var fundNamesMapping = charityFunds.ToDictionary(fund => fund.Id, fund => new { fund.name, fund.images });
            // Tạo một dictionary ánh xạ idCategory -> name từ Categorys
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
                // Kiểm tra xem idCategory có phải là null không
                if (project.idCategory != null && categoryNamesMapping.ContainsKey(project.idCategory))
                {
                    project.nameCategory = categoryNamesMapping[project.idCategory]; // Gán tên vào thuộc tính nameCategory
                }
                else
                {
                    // Nếu không có idCategory hợp lệ, bạn có thể gán giá trị mặc định hoặc bỏ qua
                    project.nameCategory = "Unknown Category"; // Hoặc bạn có thể để trống (null)
                }
                // Tính phần trăm đạt được
                if (!string.IsNullOrEmpty(project.currentAmount) &&
                    decimal.TryParse(project.currentAmount, out var currentAmount) &&
                    decimal.TryParse(project.targetAmount, out var targetAmount) &&
                    targetAmount > 0)
                {
                    project.percent = ((currentAmount / targetAmount) * 100).ToString("0.##");
                }
                else
                {
                    project.percent = "0";
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

                    // Tìm tài liệu trước khi xóa để lấy ảnh cũ, video cũ
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

                    // Nếu tài liệu tồn tại và có video, xóa tất cả video cũ
                    if (existingEntity != null && existingEntity.video != null && existingEntity.video.Count > 0)
                    {
                        // Lặp qua danh sách video và xóa từng video
                        foreach (var video in existingEntity.video)
                        {
                            // Xóa từng video
                            SaveFileHelper.DeleteProjectFile(video);
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

        //like project fund

        public async Task<bool> LikeProjectAsync(LikeProjectFund dto)
        {
            var filter = Builders<ProjectFunds>.Filter.Eq(p => p.Id, dto.projectFundId);
            var update = Builders<ProjectFunds>.Update.AddToSet(p => p.likedByUsers, dto.userId);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
        //unlike project fund
        public async Task<bool> UnlikeProjectAsync(LikeProjectFund dto)
        {
            var filter = Builders<ProjectFunds>.Filter.Eq(p => p.Id, dto.projectFundId);
            var update = Builders<ProjectFunds>.Update.Pull(p => p.likedByUsers, dto.userId);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<int> GetLikesCountAsync(string projectId)
        {
            // Chuyển đổi projectId từ string sang ObjectId
            ObjectId objectId;
            if (!ObjectId.TryParse(projectId, out objectId))
            {
                // Trường hợp nếu projectId không hợp lệ, trả về 0
                return 0;
            }
            var filter = Builders<ProjectFunds>.Filter.Eq("_id", objectId);

            // Truy vấn ProjectFunds bằng id
            var projectFunds = await _collection.Find(filter).FirstOrDefaultAsync();
            return projectFunds?.likedByUsers.Count ?? 0;
        }



    }
}

