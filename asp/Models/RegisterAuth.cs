using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace asp.Models
{
    public class RegisterAuth
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }

        [BsonElement("email")]
        public string email { get; set; } // Email của người dùng

        [BsonElement("verificationCode")]
        public string verificationCode { get; set; } // Mã xác thực gửi đến người dùng

        [BsonElement("expirationTime")]
        public DateTime expirationTime { get; set; } // Thời gian hết hạn của mã xác thực

        [BsonElement("isVerified")]
        public bool isVerified { get; set; } = false; // Trạng thái xác thực của tài khoản
    }
}
