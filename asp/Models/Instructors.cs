using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace asp.Models
{
    public class Instructors
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? user_id { get; set; }
        public string? id_khoa { get; set; }

        public string? ten_gv { get; set; }
        public string? lop { get; set; }
        public string? ky_id { get; set; }
       /* public string? ten_hoc_phan { get; set; }
        public string? ngay_bat_dau { get; set; }
        public string? ngay_ket_thuc { get; set; }
        public string? ghichu { get; set; }*/
      



    }
}
