using asp.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace asp.Respositories
{
   
    /* public class MongoDBService
     {
         private readonly IMongoCollection<Users> _booksCollection;

         public MongoDBService(
             IOptions<MongoDbSetting> bookStoreDatabaseSettings)
         {
             var mongoClient = new MongoClient(
                 bookStoreDatabaseSettings.Value.ConnectionURI);

             var mongoDatabase = mongoClient.GetDatabase(
                 bookStoreDatabaseSettings.Value.DatabaseName);

             _booksCollection = mongoDatabase.GetCollection<Users>(
                 bookStoreDatabaseSettings.Value.CollectionName);
         }

         public async Task<List<Users>> GetAsync() =>
             await _booksCollection.Find(_ => true).ToListAsync();

         public async Task<Users?> GetAsync(string id) =>
             await _booksCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

         public async Task CreateAsync(Users newBook) =>
             await _booksCollection.InsertOneAsync(newBook);

         public async Task UpdateAsync(string id, Users updatedBook) =>
             await _booksCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);

         public async Task RemoveAsync(string id) =>
             await _booksCollection.DeleteOneAsync(x => x.Id == id);
     }*/
    public class MongoDBService<T>
    {
        private readonly IMongoCollection<T> _collection;

        public MongoDBService(IOptions<MongoDbSetting> databaseSettings)
        {
            var client = new MongoClient(databaseSettings.Value.ConnectionURI);
            var database = client.GetDatabase(databaseSettings.Value.DatabaseName);
            _collection = database.GetCollection<T>(typeof(T).Name.ToLower());
        }

        public async Task<List<T>> GetAllAsync() =>
            await _collection.Find(_ => true).ToListAsync();
        public async Task<T?> GetByIdAsync(string id) =>
            await _collection.Find(Builders<T>.Filter.Eq("_id", ObjectId.Parse(id))).FirstOrDefaultAsync();

        public async Task<T> GetUserByTenDangNhapAndPassword(string tendangnhap, string matkhau)
        {
            var filter = Builders<T>.Filter.And(
                Builders<T>.Filter.Eq("tendangnhap", tendangnhap),
                Builders<T>.Filter.Eq("matkhau", matkhau)
            );

            var user = await _collection.Find(filter).FirstOrDefaultAsync();

            return user;
        }
        

        public async Task CreateAsync(T newEntity)
        {
            await _collection.InsertOneAsync(newEntity);
        }

        public async Task UpdateAsync(string id, T updatedEntity) 
{
        var filter = Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
            await _collection.ReplaceOneAsync(filter, updatedEntity);
        }

        public async Task RemoveAsync(string id) 
        {
            var filter = Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
            await _collection.DeleteOneAsync(filter);
         }


    }
}

