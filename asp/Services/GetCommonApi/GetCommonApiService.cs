using asp.Constants;
using asp.Constants.ProjectFundConst;
using asp.Helper.ConnectDb;
using asp.Helper.File;
using asp.Models;
using asp.Models.GetApiCommon;
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


    public class GetCommonApiService
    {
        private readonly IMongoCollection<CharityFunds> _collectionCharityFund;
        private readonly IMongoCollection<ProjectFunds> _collectionProjectFunds;
        private readonly IMongoCollection<MomoExecuteResponseModel> _collectionMomoCreatePaymentResponseModel;

        public GetCommonApiService(ConnectDbHelper dbHelper)
        {
            _collectionCharityFund = dbHelper.GetCollection<CharityFunds>();
            _collectionProjectFunds = dbHelper.GetCollection<ProjectFunds>();
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





    }
}

