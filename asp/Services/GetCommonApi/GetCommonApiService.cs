using asp.Constants;
using asp.Constants.ProjectFundConst;
using asp.Helper.ConnectDb;
using asp.Helper.File;
using asp.Models;
using asp.Models.GetApiCommon;
using asp.Models.ProjectFundProcessing;
using asp.Models.User;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using System.Net;
using System.Numerics;
using System.Text.RegularExpressions;

namespace asp.Services.ProjectFundDone
{


    public class GetCommonApiService
    {
        private readonly IMongoCollection<CharityFunds> _collectionCharityFund;
        private readonly IMongoCollection<ProjectFunds> _collectionProjectFunds;
        private readonly IMongoCollection<MomoExecuteResponseModel> _collectionMomoCreatePaymentResponseModel;
        private readonly IMongoCollection<Users> _collectionUser;

        public GetCommonApiService(ConnectDbHelper dbHelper)
        {
            _collectionCharityFund = dbHelper.GetCollection<CharityFunds>();
            _collectionProjectFunds = dbHelper.GetCollection<ProjectFunds>();
            _collectionUser = dbHelper.GetCollection<Users>();
            _collectionMomoCreatePaymentResponseModel = dbHelper.GetCollection<MomoExecuteResponseModel>();
        }
        //get thông tin dự án, sứ giả, tổ chức và tiền ủng hộ
        public async Task<FundsStatisticsDTO> GetFundsStatisticsAsync()
        {
            var projectFundsCount = await _collectionProjectFunds.CountDocumentsAsync(Builders<ProjectFunds>.Filter.Empty);
            var charityFundsCount = await _collectionCharityFund.CountDocumentsAsync(Builders<CharityFunds>.Filter.Empty);
            var projectFundsWithUserIdCount = await _collectionProjectFunds.CountDocumentsAsync(Builders<ProjectFunds>.Filter.Exists(p => p.userId));

            var totalMomoAmountDoc = await _collectionMomoCreatePaymentResponseModel.Aggregate()
                .Group(new BsonDocument
                {
                    { "_id", BsonNull.Value },
                    { "totalAmount", new BsonDocument("$sum", new BsonDocument("$toDouble", "$Amount")) }
                })
                .FirstOrDefaultAsync();

            var totalMomoAmount = totalMomoAmountDoc != null ? totalMomoAmountDoc["totalAmount"].ToDecimal() : 0;

            var statistics = new FundsStatisticsDTO
            {
                ProjectFundsCount = projectFundsCount,
                CharityFundsCount = charityFundsCount,
                ProjectFundsWithUserIdCount = projectFundsWithUserIdCount,
                TotalMomoAmount = totalMomoAmount
            };

            return statistics;
        }
        // lấy thông tin detail của quỹ : số lượng dự án, sứ giả, người donate, tổng tiền, list user
        public async Task<DetailFundDTO> GetDetailFundsStatisticsAsync(string fundId)
        {
            // Tạo bộ lọc để tìm các project funds theo fundId
            var projectFundsFilter = Builders<ProjectFunds>.Filter.Eq(p => p.idFund, fundId);

            // Tạo bộ lọc để tìm các project funds có userId không phải là null
            var projectFundsWithUserIdFilter = Builders<ProjectFunds>.Filter.And(
                projectFundsFilter,
                Builders<ProjectFunds>.Filter.Ne(p => p.userId, null) // Lọc các project funds có userId không phải là null
            );

            // Dùng aggregation để nhóm theo userId và đếm số lượng distinct userId
            var distinctUserIds = await _collectionProjectFunds.Aggregate()
                .Match(projectFundsWithUserIdFilter)  // Lọc theo userId không phải null
                .Group(p => p.userId, g => new { UserId = g.Key })  // Nhóm theo userId
                .Project(p => p.UserId)  // Lấy ra chỉ danh sách userId distinct
                .ToListAsync();  // Chuyển kết quả thành danh sách
            // Chuyển đổi thành List<string> và đếm số lượng distinct userId
            //var distinctUserIds = projectFundsWithDistinctUserIdCount.Select(u => u.UserId).ToList();
            //=============== Đếm số lượng userId duy nhất
            var distinctUserCount = distinctUserIds != null ? distinctUserIds.Count : 0;

            //=============== Đếm số lượng project funds
            var projectFundsCount = await _collectionProjectFunds.CountDocumentsAsync(projectFundsFilter);

            //=============== Lọc các giao dịch Momo liên quan đến các ProjectFunds tương ứng với fundId
            var projectFundIds = await _collectionProjectFunds
                .Find(projectFundsFilter)
                .Project(p => p.Id) // Lấy tất cả ProjectFundId
                .ToListAsync();

            var totalMomoAmountDoc = await _collectionMomoCreatePaymentResponseModel.Aggregate()
                .Match(Builders<MomoExecuteResponseModel>.Filter.In(m => m.ProjectFundId, projectFundIds)) // Lọc giao dịch theo ProjectFundId
                .Group(new BsonDocument
                {
            { "_id", BsonNull.Value },
            { "totalAmount", new BsonDocument("$sum", new BsonDocument("$toDouble", "$Amount")) }
                })
                .FirstOrDefaultAsync();

            //=============== Tính tổng số tiền Momo, nếu có dữ liệu
            var totalMomoAmount = totalMomoAmountDoc != null ? totalMomoAmountDoc["totalAmount"].ToDecimal() : 0;

            //=============== Tính tổng số người ủng hộ
            var totalSupportersCount = await _collectionMomoCreatePaymentResponseModel.CountDocumentsAsync(
                Builders<MomoExecuteResponseModel>.Filter.In(m => m.ProjectFundId, projectFundIds)
            );

            //=============== Lấy danh sách người dùng đã ủng hộ từ các giao dịch Momo
            
            var users = await GetUserDetailsByIdsAsync(distinctUserIds);

            // Trả về thống kê
            var statistics = new DetailFundDTO
            {
                ProjectFundsCount = projectFundsCount,
                ProjectFundsWithUserIdCount = distinctUserCount, // Đếm số lượng userId duy nhất
                TotalMomoAmount = totalMomoAmount,
                TotalSupportersCount = totalSupportersCount,
                Users = users
            };

            return statistics;
        }
        // lấy list sứ giả theo quỹ có tổng donate
        private async Task<List<UserDetailFund>> GetUserDetailsByIdsAsync(List<string> userIds)
        {
            // Lọc các giao dịch Momo theo userIds và nhóm theo userId để tính tổng số tiền donate
            var totalDonatePerUser = await _collectionMomoCreatePaymentResponseModel
                .Aggregate()
                .Match(Builders<MomoExecuteResponseModel>.Filter.In(m => m.UserId, userIds)) // Lọc giao dịch theo userIds
                .Group(new BsonDocument
                {
                    { "_id", "$UserId" }, // Nhóm theo userId
                    { "totalDonate", new BsonDocument("$sum", new BsonDocument("$toDouble", "$Amount")) } // Tính tổng số tiền donate
                })
                .ToListAsync();

            // Lọc thông tin người dùng và kết hợp với tổng số tiền donate
            var users = await _collectionUser
                .Find(Builders<Users>.Filter.In(u => u.Id, userIds))
                .ToListAsync();

            // Tạo danh sách Users kết hợp với tổng số tiền donate của từng người
            var userDetails = users.Select(u =>
            {
                // Tìm tổng số tiền donate của userId
                var userDonate = totalDonatePerUser.FirstOrDefault(d => d["_id"].AsString == u.Id);
                var totalDonate = userDonate != null ? userDonate["totalDonate"].ToDecimal() : 0;

                return new UserDetailFund
                {
                    FullName = u.fullName,
                    Avatar = u.avatar,
                    TotalDonate = totalDonate // Thêm trường totalDonate vào đối tượng Users
                };
            }).ToList();

            return userDetails;
        }







    }
}

