using asp.Constants.User;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace asp.Models.User
{
    public class Users
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("email")]
        public string? email { get; set; } = ""; // Email của người dùng

        [BsonElement("passWord")]
        public string? passWord { get; set; } = ""; // Mật khẩu của người dùng

        [BsonElement("fullName")]
        public string? fullName { get; set; } = "";  // tên

        [BsonElement("role")]
        public string? role { get; set; } = UserRole.NONE;  // phân quyền

        //public IFormFile? avatarFile { get; set; } // File avatar
        [BsonElement("isEmissary")]
        public bool? isEmissary { get; set; } = false;  // có là sứ giả

        [BsonElement("isEmissaryApproved")]
        public string? isEmissaryApproved { get; set; } = "";  // đã duyệt thành sứ giả
        public string? avatar { get; set; } = ""; // ảnh

        [BsonElement("verificationCode")]
        public string? verificationCode { get; set; } = ""; // Mã xác thực gửi đến người dùng

        [BsonElement("expirationTime")]
        public DateTime expirationTime { get; set; } = DateTime.UtcNow.AddMinutes(5);// Thời gian hết hạn của mã xác thực

        [BsonElement("isVerified")]
        public bool? isVerified { get; set; } = false; // Trạng thái xác thực của tài khoản

        [BsonElement("city")]
        public string? city { get; set; } = "";  // thành phố

        [BsonElement("district")]
        public string? district { get; set; } = "";  // quận huyện

        [BsonElement("address")]
        public string? address { get; set; } = "";  // địa chỉ

        [BsonElement("gender")]
        public string? gender { get; set; } = "";  // giới tính

        [BsonElement("ward")]
        public string? ward { get; set; } = "";  // phường xã

        [BsonElement("birthDay")]
        public string? birthDay { get; set; } = "";  // ngày sinh

        [BsonElement("phone")]
        public string? phone { get; set; } = "";  // số điện thoại

        [BsonElement("cccd1")]
        public string? cccd1 { get; set; } = "";  // mặt trước căn cước công dân

        [BsonElement("cccd2")]
        public string? cccd2 { get; set; } = "";  // mặt sau căn cước công dân

        [BsonElement("createdAt")]
        public DateTime createdAt { get; set; } = DateTime.Now;  // tạo lúc

        [BsonElement("updatedAt")]
        public DateTime updatedAt { get; set; } = DateTime.Now;  // cập nhật lúc


    }
}
