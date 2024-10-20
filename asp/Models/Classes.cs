using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace asp.Models
{
    public class Classes
    {
       
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string? nhom_id { get; set; }
        public string? nhom_name { get; set; }
        public string? nhom_admin { get; set; }
        public string? id_khoa { get; set; }
        public string? nhom_active { get; set; }

    }
}
