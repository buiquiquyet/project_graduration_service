using asp.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using System.Net;
using System.Text.RegularExpressions;

namespace asp.Respositories
{
   
   
    public class ClassService
    {
        private readonly IMongoCollection<Classes> _collection;

        public ClassService(IOptions<MongoDbSetting> databaseSettings)
        {
            var client = new MongoClient(databaseSettings.Value.ConnectionURI);
            var database = client.GetDatabase(databaseSettings.Value.DatabaseName);
            _collection = database.GetCollection<Classes>(typeof(Classes).Name.ToLower());
        }

        public async Task<List<Classes>> GetAllAsync()
        {
            return await _collection.Find(_ => true)
                                    .ToListAsync();
        }
        public async Task<long> CountAsync()
        {
                return await _collection.CountDocumentsAsync(_ => true);
        }
        public async Task<Classes?> GetByIdAsync(string id) =>
            await _collection.Find(Builders<Classes>.Filter.Eq("_id", ObjectId.Parse(id))).FirstOrDefaultAsync();
        public async Task<List<Classes>> GetByUserAsync(string tdnUser) =>
            await _collection.Find(Builders<Classes>.Filter.Eq("nhom_admin",tdnUser)).ToListAsync();
        public async Task<List<Classes>> GetByIdKhoaAsync(string idKhoa) =>
            await _collection.Find(Builders<Classes>.Filter.Eq("id_khoa", idKhoa)).ToListAsync();

        public async Task<Classes> GetUserByTenDangNhapAndPassword(string tendangnhap, string matkhau)
        {
            var filter = Builders<Classes>.Filter.And(
                Builders<Classes>.Filter.Eq("tendangnhap", tendangnhap),
                Builders<Classes>.Filter.Eq("matkhau", matkhau)
            );

            var user = await _collection.Find(filter).FirstOrDefaultAsync();

            return user;
        }
        

        public async Task CreateAsync(Classes newEntity)
        {
            await _collection.InsertOneAsync(newEntity);
        }

        public async Task UpdateAsync(string id, Classes updatedEntity) 
{
        var filter = Builders<Classes>.Filter.Eq("_id", ObjectId.Parse(id));
            await _collection.ReplaceOneAsync(filter, updatedEntity);
        }

        public async Task RemoveAsync(string id) 
        {
            var filter = Builders<Classes>.Filter.Eq("_id", ObjectId.Parse(id));
            await _collection.DeleteOneAsync(filter);
         }


    }
}

