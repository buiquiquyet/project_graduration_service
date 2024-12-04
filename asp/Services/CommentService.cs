using asp.Helper;
using asp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Xml.Linq;

namespace asp.Respositories
{


    public class CommentService
    {
        private readonly IMongoCollection<Comments> _collection;
        private readonly IMongoCollection<Users> _usersCollection;

        public CommentService(IOptions<MongoDbSetting> databaseSettings)
        {
            var client = new MongoClient(databaseSettings.Value.ConnectionURI);
            var database = client.GetDatabase(databaseSettings.Value.DatabaseName);
            _collection = database.GetCollection<Comments>(typeof(Comments).Name.ToLower());
            _usersCollection = database.GetCollection<Users>(typeof(Users).Name.ToLower());
        }

        public async Task<Comments> Create(Comments request)
        {
            
            // Tạo đối tượng ProjectFunds từ dữ liệu request
            var data = new Comments
            {
                content = request.content,
                userId = request.userId,
                projectFundId = request.projectFundId,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow,
            };

            try
            {
                // Chèn dữ liệu vào collection MongoDB
                await _collection.InsertOneAsync(data);

                // Trả về đối tượng đã chèn, bao gồm cả Id từ MongoDB nếu có
                return data;
            }
            catch (Exception ex)
            {
                // Xử lý lỗi (ghi log, ném ngoại lệ, v.v.)
                // Ví dụ: ghi log chi tiết (có thể dùng log framework như NLog, Serilog)
                throw new Exception("Có lỗi xảy ra trong quá trình chèn dữ liệu vào cơ sở dữ liệu: " + ex.Message, ex);
            }
        }
        // lấy 1 list bình luận theo id của project fund
        //public async Task<List<Comments>> GetCommentsByProjectFundIdAsync(string projectFundId, int skipAmount, int pageSize)
        //{
        //    var sortDefinition = Builders<Comments>.Sort.Descending(x => x.Id);
        //    return await _collection.Find(comment => comment.projectFundId == projectFundId)
        //                .Skip(skipAmount)
        //                .Sort(sortDefinition)
        //                .Limit(pageSize)
        //                .ToListAsync();
        //}
        public async Task<List<Comments>> GetCommentsByProjectFundIdAsync(string projectFundId, int skipAmount, int pageSize)
        {
            // Lấy danh sách các comment dựa theo projectFundId
            var comments = await _collection
                .Find(comment => comment.projectFundId == projectFundId)
                .Skip(skipAmount)
                .Limit(pageSize)
                .SortByDescending(comment => comment.createdAt)
                .ToListAsync();

            // Lấy danh sách userId duy nhất từ comments
            var userIds = comments.Select(comment => comment.userId).Distinct().ToList();

            // Lấy thông tin người dùng từ collection "Users"
            var users = await _usersCollection
                .Find(user => userIds.Contains(user.Id))
                .ToListAsync();

            // Kết hợp thông tin người dùng vào các comment
            foreach (var comment in comments)
            {
                var user = users.FirstOrDefault(u => u.Id == comment.userId);
                if (user != null)
                {
                    comment.userName = user.fullName; // Gán tên người dùng
                    comment.userAvatar = user.avatar; // Gán avatar người dùng
                }
            }

            return comments;
        }


        // đếm số lượng bản ghi
        public async Task<long> CountAsync(string projectFundId)
        {
            return await _collection.CountDocumentsAsync(comment => comment.projectFundId == projectFundId);
        }


    }
}

