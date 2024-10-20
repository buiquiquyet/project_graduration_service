using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace asp.Models
{
    public class Departments
    {
       
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? id_khoa { get; set; }
        public string? name_khoa { get; set; }
       
    }
}
