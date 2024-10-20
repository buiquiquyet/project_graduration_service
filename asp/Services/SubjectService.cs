using asp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;

namespace asp.Respositories
{


    public class SubjectService
    {
        private readonly IMongoCollection<Subjects> _collection;

        public SubjectService(IOptions<MongoDbSetting> databaseSettings)
        {
            var client = new MongoClient(databaseSettings.Value.ConnectionURI);
            var database = client.GetDatabase(databaseSettings.Value.DatabaseName);
            _collection = database.GetCollection<Subjects>(typeof(Subjects).Name.ToLower());
        }

        public async Task<List<Subjects>> GetAllAsync()
        {
            return await _collection.Find(_ => true)
                                    .ToListAsync();
        }
        public async Task<long> CountAsync()
        {
            return await _collection.CountDocumentsAsync(_ => true);
        }
            public async Task<Subjects?> GetByIdAsync(string id) =>
                await _collection.Find(Builders<Subjects>.Filter.Eq("_id", ObjectId.Parse(id))).FirstOrDefaultAsync();

        public async Task<Subjects> GetByUserIdAsync(string userTdn)
        {
           /* var filter = Builders<Subjects>.Filter.Eq("user_id", userId);
            return await _collection.Find(filter)
                                    .FirstOrDefaultAsync();*/
            return await _collection.Find(Builders<Subjects>.Filter.Eq("user_id", userTdn)).FirstOrDefaultAsync();

        }
        public async Task<List<Subjects>> GetByDepartmentIdAsync(string departmentId)
        {
            var filter = Builders<Subjects>.Filter.Eq("id_khoa", departmentId);
            return await _collection.Find(filter)
                                    .ToListAsync();

        }
       
    }
}

