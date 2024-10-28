using asp.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net.Mail;
using System.Net;
using asp.Helper;
using asp.DTO;
using System.Text.RegularExpressions;

namespace asp.Respositories
{


    public class RegisterAuthService
    {
        private readonly IMongoCollection<RegisterAuth> _registerAuthCollection;
        public string GenerateVerificationCode()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }
        public void SendVerificationEmail(string userEmail, string verificationCode)
        {
            if (string.IsNullOrEmpty(userEmail))
            {
                throw new ArgumentNullException(nameof(userEmail), "Email người dùng không được để trống.");
            }

            if (string.IsNullOrEmpty(SendMailConstant.emailSender))
            {
                throw new ArgumentNullException(nameof(SendMailConstant.emailSender), "Email người gửi không được để trống.");
            }

            // Kiểm tra xem địa chỉ email người gửi có hợp lệ không
            if (!IsValidEmail(SendMailConstant.emailSender))
            {
                throw new ArgumentException("Địa chỉ email không hợp lệ.", nameof(SendMailConstant.emailSender));
            }

            // Tạo đối tượng MailMessage
            MailMessage mailMessage;
            try
            {
                mailMessage = new MailMessage
                {
                    From = new MailAddress(SendMailConstant.emailSender), // Địa chỉ email của bạn
                    Subject = "Mã xác thực của bạn - Hãy xác thực ngay!",
                    Body = SendMailConstant.bodyEmail(verificationCode),
                    IsBodyHtml = true,
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi tạo đối tượng MailMessage: " + ex.Message);
            }

            // Thêm người nhận vào email
            mailMessage.To.Add(userEmail);

            using (var smtpClient = new SmtpClient())
            {
                // Thiết lập thông tin xác thực
                smtpClient.Credentials = new NetworkCredential(SendMailConstant.emailSender, SendMailConstant.passwordSender);
                smtpClient.EnableSsl = true; // Bật SSL để bảo mật
                smtpClient.Host = SendMailConstant.hostEmail; // Host của máy chủ SMTP
                smtpClient.Port = SendMailConstant.portEmail; // Cổng của máy chủ SMTP
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network; // Phương thức gửi email
                smtpClient.UseDefaultCredentials = false; // Không sử dụng thông tin xác thực mặc định

                try
                {
                    // Gửi email
                    smtpClient.Send(mailMessage);
                }
                catch (SmtpException smtpEx)
                {
                    // Xử lý lỗi liên quan đến SMTP
                    throw new Exception("Lỗi gửi email: " + smtpEx.Message);
                }
                catch (Exception ex)
                {
                    // Xử lý lỗi tổng quát
                    throw new Exception("Có lỗi xảy ra trong quá trình gửi email: " + ex.Message);
                }
            }

        }
        // kiểm tra validate email
        private bool IsValidEmail(string email)
        {
            // Sử dụng biểu thức chính quy để kiểm tra định dạng email
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return emailRegex.IsMatch(email);
        }
        //lưu mã xác thực vào cơ sở dữ liệu
        public async Task Create(string email, string verificationCode)
        {
            var registerAuth = new RegisterAuth
            {
                email = email,
                verificationCode = verificationCode,
                expirationTime = DateTime.UtcNow.AddMinutes(5), // Mã xác thực sẽ hết hạn sau 5 phút
                isVerified = false
            };
            await _registerAuthCollection.InsertOneAsync(registerAuth); // Chèn dữ liệu vào collection
        }
        // kiểm tra mã xác thực có hợp lệ không
        public bool CheckVerificationCode(string email, string code)
        {
            // Lấy bản ghi từ MongoDB với email và mã xác thực khớp
            var filter = Builders<RegisterAuth>.Filter.And(
                Builders<RegisterAuth>.Filter.Eq(x => x.email, email),
                Builders<RegisterAuth>.Filter.Eq(x => x.verificationCode, code)
            );

            var registerAuth = _registerAuthCollection.Find(filter).FirstOrDefault();

            // Kiểm tra nếu mã tồn tại và chưa hết hạn
            if (registerAuth != null && registerAuth.expirationTime > DateTime.UtcNow)
            {
                // Cập nhật trạng thái xác thực
                var update = Builders<RegisterAuth>.Update.Set(x => x.isVerified, true);
                _registerAuthCollection.UpdateOne(filter, update);

                return true; // Mã hợp lệ
            }

            return false; // Mã không hợp lệ hoặc đã hết hạn
        }
    }
}

