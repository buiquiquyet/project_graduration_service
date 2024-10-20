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
        public string? tendangnhap { get; set; }
        public string? matkhau { get; set; }
        public string? id_khoa { get; set; }
        public string? nhom_id { get; set; }
        public string? lop { get; set; }
        public string? hodem { get; set; }
        public string? ten { get; set; }
        public string? gioitinh { get; set; }
        public string? ngaysinh { get; set; }
        public string? anh { get; set; }
        public string? dienthoai { get; set; }
        public string? email { get; set; }
        public string? diachi { get; set; }
        public string? active { get; set; }
        public string? quyen { get; set; }
    }
}
