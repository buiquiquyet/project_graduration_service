using asp.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using System.Net;
using System.Text.RegularExpressions;

namespace asp.Respositories
{
   
   
    public class DepartmentService
    {
        private readonly IMongoCollection<Departments> _collection;

        public DepartmentService(IOptions<MongoDbSetting> databaseSettings)
        {
            var client = new MongoClient(databaseSettings.Value.ConnectionURI);
            var database = client.GetDatabase(databaseSettings.Value.DatabaseName);
            _collection = database.GetCollection<Departments>(typeof(Departments).Name.ToLower());
        }

        public async Task<List<Departments>> GetAllAsync()
        {
            return await _collection.Find(_ => true)
                                    .ToListAsync();
        }
        public async Task<long> CountAsync()
        {
                return await _collection.CountDocumentsAsync(_ => true);
        }
        public async Task<Departments?> GetByIdAsync(string id) =>
            await _collection.Find(Builders<Departments>.Filter.Eq("_id", ObjectId.Parse(id))).FirstOrDefaultAsync();
        public async Task<Departments?> GetByIdDepartmentAsync(string idDepartment) =>
            await _collection.Find(Builders<Departments>.Filter.Eq("id_khoa",idDepartment)).FirstOrDefaultAsync();
        public async Task<Departments> GetUserByTenDangNhapAndPassword(string tendangnhap, string matkhau)
        {
            var filter = Builders<Departments>.Filter.And(
                Builders<Departments>.Filter.Eq("tendangnhap", tendangnhap),
                Builders<Departments>.Filter.Eq("matkhau", matkhau)
            );

            var department = await _collection.Find(filter).FirstOrDefaultAsync();

            return department;
        }
        

        public async Task CreateAsync(Departments newEntity)
        {
            await _collection.InsertOneAsync(newEntity);
        }

        public async Task UpdateAsync(string id, Departments updatedEntity) 
{
        var filter = Builders<Departments>.Filter.Eq("_id", ObjectId.Parse(id));
            await _collection.ReplaceOneAsync(filter, updatedEntity);
        }

        public async Task RemoveAsync(string id) 
        {
            var filter = Builders<Departments>.Filter.Eq("_id", ObjectId.Parse(id));
            await _collection.DeleteOneAsync(filter);
         }


    }
}

