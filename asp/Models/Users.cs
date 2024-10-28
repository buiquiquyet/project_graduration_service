using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace asp.Models
{
    public class Users
    {
        /*public Users()
        {
        }*/
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        //public string? tendangnhap { get; set; }
        public string? pass { get; set; }
        //public string? id_khoa { get; set; }
        //public string? nhom_id { get; set; }
        //public string? lop { get; set; }
        //public string? hoten { get; set; }
        public string? fullName { get; set; }
        public string? gender { get; set; }
        public string? birthDay { get; set; }
        public string? avatar { get; set; }
        public string? phone { get; set; }
        public string? email { get; set; }
        public string? city { get; set; }
        public string? district { get; set; }
        public string? ward { get; set; }
        //public string? diachi { get; set; }
        //public string? active { get; set; }
        public string? role { get; set; }
        public string? createDate { get; set; }
    }
}
