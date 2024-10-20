using asp.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using System.Net;
using System.Text.RegularExpressions;

namespace asp.Respositories
{


    public class SemesterService
    {
        private readonly IMongoCollection<Semesters> _collection;

        public SemesterService(IOptions<MongoDbSetting> databaseSettings)
        {
            var client = new MongoClient(databaseSettings.Value.ConnectionURI);
            var database = client.GetDatabase(databaseSettings.Value.DatabaseName);
            _collection = database.GetCollection<Semesters>(typeof(Semesters).Name.ToLower());
        }
        public async Task<List<Semesters>> GetAllAsync()
        {

            return await _collection.Find(_ => true)
                                    .ToListAsync();
        }
        public async Task<Semesters?> GetByIdAsync(string id) =>
            await _collection.Find(Builders<Semesters>.Filter.Eq("_id", ObjectId.Parse(id))).FirstOrDefaultAsync();
       /* public async Task<Records?> GetByIdRecordstAsync(string idRecord) =>
            await _collection.Find(Builders<Records>.Filter.Eq("id_khoa", idRecord)).FirstOrDefaultAsync();*/

       /* public async Task CreateAsync(Records newEntity)
        {
            await _collection.InsertOneAsync(newEntity);
        }*/

       /* public async Task UpdateAsync(string id, Records updatedEntity)
        {
            var filter = Builders<Records>.Filter.Eq("_id", ObjectId.Parse(id));
            await _collection.ReplaceOneAsync(filter, updatedEntity);
        }

        public async Task RemoveAsync(string id)
        {
            var filter = Builders<Records>.Filter.Eq("_id", ObjectId.Parse(id));
            await _collection.DeleteOneAsync(filter);
        }
        public async Task<long> DeleteByIdsAsync(List<string> ids)
        {
            var filter = Builders<Records>.Filter.In("_id", ids.Select(ObjectId.Parse));
            var result = await _collection.DeleteManyAsync(filter);
            return result.DeletedCount;
        }*/


    }
}

