using asp.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using System.Net;
using System.Text.RegularExpressions;

namespace asp.Respositories
{
   
   
    public class UserService
    {
        private readonly IMongoCollection<Users> _collection;

        public UserService(IOptions<MongoDbSetting> databaseSettings)
        {
            var client = new MongoClient(databaseSettings.Value.ConnectionURI);
            var database = client.GetDatabase(databaseSettings.Value.DatabaseName);
            _collection = database.GetCollection<Users>(typeof(Users).Name.ToLower());
        }

        public async Task<List<Users>> GetAllAsync(int skipAmount, int pageSize)
        {
            var sortDefinition = Builders<Users>.Sort.Descending(x => x.Id); 

            return await _collection.Find(_ => true)
                                    .Skip(skipAmount)
                                    .Sort(sortDefinition)
                                    .Limit(pageSize)
                                    .ToListAsync();
        }
        public async Task<long> CountAsync()
        {
                return await _collection.CountDocumentsAsync(_ => true);
        }
        public async Task<Users?> GetByIdAsync(string id) =>
            await _collection.Find(Builders<Users>.Filter.Eq("_id", ObjectId.Parse(id))).FirstOrDefaultAsync();
        public async Task<Users?> GetByTenDangNhapAsync(string tendangnhap) =>
            await _collection.Find(Builders<Users>.Filter.Eq("tendangnhap", tendangnhap)).FirstOrDefaultAsync();
        public async Task<List<Users>> GetByIdDepartmentAsync(string id) =>
           await _collection.Find(Builders<Users>.Filter.Eq("id_khoa", id)).ToListAsync();

        public async Task<Users> GetUserByTenDangNhapAndPassword(string tendangnhap, string matkhau)
        {
            var filter = Builders<Users>.Filter.And(
                Builders<Users>.Filter.Eq("tendangnhap", tendangnhap),
                Builders<Users>.Filter.Eq("matkhau", matkhau)
            );

            var user = await _collection.Find(filter).FirstOrDefaultAsync();

            return user;
        }
        

        public async Task CreateAsync(Users newEntity)
        {
            await _collection.InsertOneAsync(newEntity);
        }
        public async Task<long> CreatetManyAsync(List<Users> entities)
        {
            try
            {
                await _collection.InsertManyAsync(entities);
                return entities.Count;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi chèn nhiều người dùng.", ex);
            }
        }

        public async Task UpdateAsync(string id, Users updatedEntity) 
{
        var filter = Builders<Users>.Filter.Eq("_id", ObjectId.Parse(id));
            await _collection.ReplaceOneAsync(filter, updatedEntity);
        }

        public async Task RemoveAsync(string id) 
        {
            var filter = Builders<Users>.Filter.Eq("_id", ObjectId.Parse(id));
            await _collection.DeleteOneAsync(filter);
         }
        public async Task<long> DeleteByIdsAsync(List<string> ids)
        {
            var filter = Builders<Users>.Filter.In("_id", ids.Select(ObjectId.Parse));
            var result = await _collection.DeleteManyAsync(filter);
            return result.DeletedCount;
        }


    }
}

