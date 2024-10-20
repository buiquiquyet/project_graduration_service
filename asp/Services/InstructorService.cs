using asp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
namespace asp.Respositories
{


    public class InstructorService
    {
        private readonly IMongoCollection<Instructors> _collection;

        public InstructorService(IOptions<MongoDbSetting> databaseSettings)
        {
            var client = new MongoClient(databaseSettings.Value.ConnectionURI);
            var database = client.GetDatabase(databaseSettings.Value.DatabaseName);
            _collection = database.GetCollection<Instructors>(typeof(Instructors).Name.ToLower());
        }

        public async Task<List<Instructors>> GetAllAsync(int skipAmount, int pageSize)
        {
            var sortDefinition = Builders<Instructors>.Sort.Descending(x => x.Id);

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
        public async Task<Instructors?> GetByIdAsync(string id) =>
            await _collection.Find(Builders<Instructors>.Filter.Eq("_id", ObjectId.Parse(id))).FirstOrDefaultAsync();
        /* public async Task<Records?> GetByIdRecordstAsync(string idRecord) =>
             await _collection.Find(Builders<Records>.Filter.Eq("id_khoa", idRecord)).FirstOrDefaultAsync();*/
        public async Task<List<Instructors>> GetByUserIdAsync(string userId, int skipAmount, int pageSize)
        {
            var filter = Builders<Instructors>.Filter.Eq("user_id", userId);
            var sortDefinition = Builders<Instructors>.Sort.Descending(x => x.Id);

            return await _collection.Find(filter)
                                    .Skip(skipAmount)
                                    .Sort(sortDefinition)
                                    .Limit(pageSize)
                                    .ToListAsync();

        }
        public async Task<List<Instructors>> GetByDepartmentIdAsync(string departmentId, int skipAmount, int pageSize)
        {
            var filter = Builders<Instructors>.Filter.Eq("id_khoa", departmentId);
            var sortDefinition = Builders<Instructors>.Sort.Descending(x => x.Id);

            return await _collection.Find(filter)
                                    .Skip(skipAmount)
                                    .Sort(sortDefinition)
                                    .Limit(pageSize)
                                    .ToListAsync();

        }
        public async Task<String> CreateAsync(Instructors newEntity)
        {
            await _collection.InsertOneAsync(newEntity);

            return newEntity.Id;
        }

        public async Task UpdateAsync(string id, Instructors updatedEntity)
        {
            var filter = Builders<Instructors>.Filter.Eq("_id", ObjectId.Parse(id));
            await _collection.ReplaceOneAsync(filter, updatedEntity);
        }

        public async Task RemoveAsync(string id)
        {
            var filter = Builders<Instructors>.Filter.Eq("_id", ObjectId.Parse(id));
            await _collection.DeleteOneAsync(filter);
        }
        public async Task<long> DeleteByIdsAsync(List<string> ids)
        {
            var filter = Builders<Instructors>.Filter.In("_id", ids.Select(ObjectId.Parse));
            var result = await _collection.DeleteManyAsync(filter);
            return result.DeletedCount;
        }


    }
}

