using asp.Models.MongoSetting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace asp.Helper.ConnectDb
{
    public class ConnectDbHelper
    {
        private readonly IMongoDatabase _database;

        public ConnectDbHelper(IOptions<MongoDbSetting> databaseSettings)
        {
            var client = new MongoClient(databaseSettings.Value.ConnectionURI);
            _database = client.GetDatabase(databaseSettings.Value.DatabaseName);
        }

        // Hàm lấy collection
        public IMongoCollection<T> GetCollection<T>()
        {
            return _database.GetCollection<T>(typeof(T).Name.ToLower());
        }
    }
}
