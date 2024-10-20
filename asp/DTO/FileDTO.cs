using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace asp.DTO
{
    public class FileDTO
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? profile_id { get; set; }
        public IFormFile? ten { get; set; }
    }
}
