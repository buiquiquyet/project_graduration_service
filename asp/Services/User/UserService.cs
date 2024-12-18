using asp.Constants;
using asp.Helper.ConnectDb;
using asp.Helper.File;
using asp.Models.User;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace asp.Services.User
{


    public class UserService
    {
        private readonly HttpClient _httpClient;
        private readonly IMongoCollection<Users> _collection;
        private readonly IMongoCollection<MomoExecuteResponseModel> _collectionMomo;
        private readonly IMongoCollection<ProjectFunds> _collectionProjectFunds;
        public UserService(ConnectDbHelper dbHelper, HttpClient httpClient)
        {
            _collection = dbHelper.GetCollection<Users>();
            _collectionMomo = dbHelper.GetCollection<MomoExecuteResponseModel>();
            _collectionProjectFunds = dbHelper.GetCollection<ProjectFunds>();
            _httpClient = httpClient;
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

            // Tạo filter để tìm tài liệu cần cập nhật theo _id
            var filter = Builders<Users>.Filter.Eq("_id", ObjectId.Parse(id));

            // Tìm tài liệu trước khi cập nhật để lấy ảnh cũ (nếu có)
            var existingEntity = await _collection.Find(filter).FirstOrDefaultAsync();

            // Danh sách các cập nhật
            var updates = new List<UpdateDefinition<Users>>();

            // Tạo một phương thức để thêm các trường vào danh sách cập nhật chỉ khi chúng khác null hoặc không rỗng
            void AddUpdate(Expression<Func<Users, object>> field, object value)
            {
                if (value != null && !string.IsNullOrEmpty(value.ToString())) // Kiểm tra null hoặc chuỗi rỗng
                {
                    updates.Add(Builders<Users>.Update.Set(field, value));
                }
            }
            

            // Thêm các trường vào danh sách cập nhật
            AddUpdate(x => x.fullName, updatedEntity.fullName);
            AddUpdate(x => x.email, updatedEntity.email);
            AddUpdate(x => x.phone, updatedEntity.phone);
            AddUpdate(x => x.address, updatedEntity.address);
            AddUpdate(x => x.city, updatedEntity.city);
            AddUpdate(x => x.district, updatedEntity.district);
            AddUpdate(x => x.ward, updatedEntity.ward);
            AddUpdate(x => x.birthDay, updatedEntity.birthDay);
            AddUpdate(x => x.gender, updatedEntity.gender);
            AddUpdate(x => x.updatedAt, DateTime.UtcNow);
            // Cập nhật isEmissaryApproved chỉ khi updatedEntity.isEmissaryApproved là rỗng
            if (updatedEntity.isEmissaryApproved == "")
            {
                // Nếu isEmissaryApproved rỗng và isEmissary là true, cập nhật là "processing"
                AddUpdate(x => x.isEmissaryApproved, existingEntity.isEmissary == true ? ApprovedConst.PROCESSING : null);
            }
            // Xử lý ảnh mới nếu có
            if (updatedEntity.cccdIFormFile != null && updatedEntity.cccdIFormFile.Count > 0)
            {
                // Lưu tất cả các ảnh mới và lấy đường dẫn của các ảnh
                var newImageFilePaths = new List<string>();
                foreach (var imageFile in updatedEntity.cccdIFormFile)
                {
                    var newImageFilePath = await SaveFileHelper.SaveFileAsync(imageFile);
                    newImageFilePaths.Add(newImageFilePath);
                }

                // Nếu có ảnh cũ trong cơ sở dữ liệu và có ảnh mới, xóa ảnh cũ
                if (existingEntity != null && existingEntity.cccd != null && existingEntity.cccd.Count > 0)
                {
                    foreach (var oldImage in existingEntity.cccd)
                    {
                        // Xóa ảnh cũ
                        SaveFileHelper.DeleteProjectFile(oldImage);
                    }
                }

                // Cập nhật file ảnh mới
                updates.Add(Builders<Users>.Update.Set(x => x.cccd, newImageFilePaths));
            }

            // Kết hợp tất cả các cập nhật thành một UpdateDefinition
            var updateDefinition = Builders<Users>.Update.Combine(updates);

            // Thực hiện cập nhật tài liệu
            var result = await _collection.UpdateOneAsync(filter, updateDefinition);

            // Kiểm tra xem có tài liệu nào được cập nhật không
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
        public async Task<List<Users>> GetAllAsync(int skipAmount, int pageSize, string searchValue = null)
        {
            var sortDefinition = Builders<Users>.Sort.Descending(x => x.Id);

            // Tạo bộ lọc tìm kiếm mặc định (không có điều kiện)
            var filter = Builders<Users>.Filter.Empty;

            if (!string.IsNullOrEmpty(searchValue))
            {
                // Tạo bộ lọc tìm kiếm gần đúng cho trường fullName (dùng Regex với cả dấu và không dấu)
                var searchFilter = Builders<Users>.Filter.Or(
                    Builders<Users>.Filter.Regex(x => x.fullName, new BsonRegularExpression($"(?i).*{searchValue}.*")), // Tìm kiếm không dấu, không phân biệt hoa thường
                    Builders<Users>.Filter.Regex(x => x.fullName, new BsonRegularExpression($"(?i).*{searchValue.ToLowerInvariant()}.*")) // Tìm kiếm có dấu, không phân biệt hoa thường
                );

                // Áp dụng bộ lọc tìm kiếm gần đúng
                filter = Builders<Users>.Filter.And(filter, searchFilter);
            }

            // Lấy danh sách Users với các trang (skip và limit)
            var users = await _collection.Find(filter)
                                         .Skip(skipAmount)
                                         .Sort(sortDefinition)
                                         .Limit(pageSize)
                                         .ToListAsync();

            return users;
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

        // lịch sử donate
        public async Task<(List<MomoExecuteResponseModel> donates, long totalCount)>  GetDonatesByUserIdAsync(string userId, int skipAmount, int pageSize, string searchValue)
        {
            var filterBuilder = Builders<MomoExecuteResponseModel>.Filter;
            FilterDefinition<MomoExecuteResponseModel> filter = filterBuilder.Eq(donate => donate.UserId, userId);

            // Kiểm tra nếu searchValue có giá trị để tìm kiếm theo tên quỹ hoặc số tiền
            if (!string.IsNullOrEmpty(searchValue))
            {
                // Tìm kiếm theo tên quỹ (ProjectName) với Regex
                var regexProjectNameFilter = filterBuilder.Regex(donate => donate.ProjectName, new BsonRegularExpression(searchValue, "i"));

                // Tìm kiếm theo số tiền (Amount) với Regex
                var regexAmountFilter = filterBuilder.Regex(donate => donate.Amount, new BsonRegularExpression(searchValue, "i"));

                // Kết hợp cả hai điều kiện bằng toán tử OR (tìm kiếm theo ProjectName hoặc Amount)
                filter &= filterBuilder.Or(regexProjectNameFilter, regexAmountFilter);
            }
            // Nếu searchValue là rỗng, không thêm điều kiện nào vào filter
            // Đếm tổng số lượng donate thỏa mãn filter
            var totalCount = await _collectionMomo.CountDocumentsAsync(filter);
            // Lấy danh sách các donate dựa theo điều kiện tìm kiếm
            var donates = await _collectionMomo
                .Find(filter)
                .Skip(skipAmount)
                .Limit(pageSize)
                .SortByDescending(donate => donate.CreatedAt)
                .ToListAsync();

            // Lấy danh sách ProjectFundId duy nhất từ donate
            var projectFundIds = donates
                .Select(donate => donate.ProjectFundId)
                .Distinct()
                .ToList();

            // Lấy thông tin từ collection ProjectFunds
            var projectFunds = await _collectionProjectFunds
                .Find(pf => projectFundIds.Contains(pf.Id))
                .ToListAsync();

            // Tạo dictionary để truy xuất nhanh thông tin project fund
            var projectFundDict = projectFunds.ToDictionary(pf => pf.Id);

            // Kết hợp thông tin project fund vào các donate
            foreach (var donate in donates)
            {
                if (projectFundDict.TryGetValue(donate.ProjectFundId, out var projectFund))
                {
                    donate.ProjectName = projectFund.name;
                    donate.ProjectNameFund = projectFund.nameFund;
                    donate.ProjectNameCategory = projectFund.nameCategory;
                    donate.ProjectTargetAmount = projectFund.targetAmount;
                    donate.ProjectStartDate = projectFund.startDate;
                    donate.ProjectCurrentAmount = projectFund.currentAmount;
                    donate.ProjectEndDate = projectFund.endDate;
                }
            }

            // Lấy danh sách userId duy nhất từ donate và chỉ giữ lại các ObjectId hợp lệ
            var userIds = donates
                .Select(donate => donate.UserId)
                .Where(userId => IsValidObjectId(userId))
                .Distinct()
                .ToList();

            // Lấy thông tin người dùng từ collection "Users"
            var users = await _collection
                .Find(user => userIds.Contains(user.Id))
                .ToListAsync();

            // Tạo dictionary để truy xuất nhanh thông tin người dùng
            var userDict = users.ToDictionary(u => u.Id);

            // Kết hợp thông tin người dùng vào các donate
            foreach (var donate in donates)
            {
                if (userDict.TryGetValue(donate.UserId, out var user))
                {
                    donate.FullName = user.fullName;
                }
                else
                {
                    donate.FullName = "Nhà hảo tâm ẩn danh"; // Gán tên người dùng mặc định
                }
            }

            return (donates, totalCount);
        }

        private bool IsValidObjectId(string objectId)
        {
            return ObjectId.TryParse(objectId, out _);
        }


        // lấy danh sách người dùng duyệt sứ giả
        public async Task<List<Users>> GetAllUserEmissaryAsync(int skipAmount, int pageSize, string searchValue = null, string isEmissaryApproved = ApprovedConst.PROCESSING)
        {
            var sortDefinition = Builders<Users>.Sort.Descending(x => x.Id);

            // Tạo bộ lọc tìm kiếm mặc định (không có điều kiện)
            var filter = Builders<Users>.Filter.Empty;

            // Nếu có giá trị tìm kiếm, tạo bộ lọc tìm kiếm gần đúng cho trường fullName
            if (!string.IsNullOrEmpty(searchValue))
            {
                var searchFilter = Builders<Users>.Filter.Or(
                    Builders<Users>.Filter.Regex(x => x.fullName, new BsonRegularExpression($"(?i).*{searchValue}.*")),
                    Builders<Users>.Filter.Regex(x => x.fullName, new BsonRegularExpression($"(?i).*{searchValue.ToLowerInvariant()}.*"))
                );

                // Áp dụng bộ lọc tìm kiếm gần đúng
                filter = Builders<Users>.Filter.And(filter, searchFilter);
            }

            // Nếu có điều kiện isEmissaryApproved, thêm vào bộ lọc
            if (!string.IsNullOrEmpty(isEmissaryApproved))
            {
                var approvalFilter = Builders<Users>.Filter.Eq(x => x.isEmissaryApproved, isEmissaryApproved);
                filter = Builders<Users>.Filter.And(filter, approvalFilter);
            }

            // Lấy danh sách Users với các trang (skip và limit)
            var users = await _collection.Find(filter)
                                          .Skip(skipAmount)
                                          .Sort(sortDefinition)
                                          .Limit(pageSize)
                                          .ToListAsync();

            // Lấy tên city, district, ward, gender cho mỗi người dùng
            var updatedUsers = new List<Users>();

            var tasks = users.Select(async user =>
            {

                // Chuyển đổi gender thành 'nam' hoặc 'nữ'
                user.gender = user.gender switch
                {
                    "male" => "nam",
                    "female" => "nữ",
                    _ => user.gender
                };

                return user;
            }).ToList();

            // Chờ tất cả các task hoàn thành
            updatedUsers = (await Task.WhenAll(tasks)).ToList();

            return updatedUsers;
        }




        //public async Task<List<Users>> GetAllUserEmissaryAsync(int skipAmount, int pageSize, string searchValue = null, string isEmissaryApproved = ApprovedConst.PROCESSING)
        //{
        //    var sortDefinition = Builders<Users>.Sort.Descending(x => x.Id);

        //    // Tạo bộ lọc tìm kiếm mặc định (không có điều kiện)
        //    var filter = Builders<Users>.Filter.Empty;

        //    // Nếu có giá trị tìm kiếm, tạo bộ lọc tìm kiếm gần đúng cho trường fullName
        //    if (!string.IsNullOrEmpty(searchValue))
        //    {
        //        var searchFilter = Builders<Users>.Filter.Or(
        //            Builders<Users>.Filter.Regex(x => x.fullName, new BsonRegularExpression($"(?i).*{searchValue}.*")),
        //            Builders<Users>.Filter.Regex(x => x.fullName, new BsonRegularExpression($"(?i).*{searchValue.ToLowerInvariant()}.*"))
        //        );

        //        // Áp dụng bộ lọc tìm kiếm gần đúng
        //        filter = Builders<Users>.Filter.And(filter, searchFilter);
        //    }

        //    // Nếu có điều kiện isEmissaryApproved, thêm vào bộ lọc
        //    if (!string.IsNullOrEmpty(isEmissaryApproved))
        //    {
        //        var approvalFilter = Builders<Users>.Filter.Eq(x => x.isEmissaryApproved, isEmissaryApproved);
        //        filter = Builders<Users>.Filter.And(filter, approvalFilter);
        //    }

        //    // Lấy danh sách Users với các trang (skip và limit)
        //    var users = await _collection.Find(filter)
        //                                  .Skip(skipAmount)
        //                                  .Sort(sortDefinition)
        //                                  .Limit(pageSize)
        //                                  .ToListAsync();

        //    return users;
        //}

        // update trạng thái duyệt sứ giả
        public async Task UpdateIsEmissaryApprovedAsync(List<string> userIds, string newApprovalStatus)
        {
            // Tạo bộ lọc để tìm các người dùng có userId trong danh sách
            var filter = Builders<Users>.Filter.In(x => x.Id, userIds);

            // Tạo cập nhật cho trường isEmissaryApproved
            var update = Builders<Users>.Update.Set(x => x.isEmissaryApproved, newApprovalStatus);

            // Thực hiện cập nhật cho tất cả người dùng khớp với bộ lọc
            await _collection.UpdateManyAsync(filter, update);
        }


        

    }
}

