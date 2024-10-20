using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Text.Json.Serialization;

namespace asp.Models
{
    public class Subjects
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? user_id { get; set; }
        public string? id_khoa { get; set; }
        public string? ten { get; set; }




    }
}
