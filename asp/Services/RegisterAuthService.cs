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

        public RegisterAuthService(ConnectDbHelper dbHelper)
        {
            _registerAuthCollection = dbHelper.GetCollection<RegisterAuth>();
        }
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

            if (SendMailConstant.bodyEmail == null)
            {
                throw new ArgumentNullException(nameof(SendMailConstant.bodyEmail), "Template email không được để trống.");
            }

            // Kiểm tra xem địa chỉ email người gửi có hợp lệ không
            if (!IsValidEmail(SendMailConstant.emailSender))
            {
                throw new ArgumentException("Địa chỉ email không hợp lệ.", nameof(SendMailConstant.emailSender));
            }

            // Tạo đối tượng MailMessage
            MailMessage mailMessage = null;
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

        //==================
        // Hàm kiểm tra xem verificationCode có tồn tại trong DB không
        public async Task<bool> VerificationCodeExistsAsync(string verificationCode)
        {
            if (string.IsNullOrEmpty(verificationCode))
            {
                throw new ArgumentNullException(nameof(verificationCode), "Mã xác thực không được để trống.");
            }

            // Tìm kiếm trong DB
            var existingRecord = await _registerAuthCollection
                .Find(r => r.verificationCode == verificationCode)
                .FirstOrDefaultAsync();

            return existingRecord != null; // Trả về true nếu tìm thấy
        }
        //==================
        // Hàm kiểm tra xem email có tồn tại trong DB không
        public async Task<bool> EmailExistsAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentNullException(nameof(email), "Email không được để trống.");
            }

            // Tìm kiếm trong DB
            var existingRecord = await _registerAuthCollection
                .Find(r => r.email == email)
                .FirstOrDefaultAsync();

            // Trả về true nếu tìm thấy email
            return existingRecord != null;
        }
        // update lại DTO email sau khi check email tồn tại và hết thời gian hiệu lực
        public async Task<bool> UpdateVerificationCodeIfExpiredAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentNullException(nameof(email), "Email không được để trống.");
            }

            // Tìm kiếm bản ghi email trong DB
            var existingRecord = await _registerAuthCollection
                .Find(r => r.email == email)
                .FirstOrDefaultAsync();

            // Kiểm tra nếu tồn tại email
            if (existingRecord != null)
            {
                // Kiểm tra xem expirationTime có vượt quá 20 giây hay không
                if (existingRecord.expirationTime < DateTime.UtcNow)
                {
                    // Nếu hết hạn, tạo mã xác thực mới và cập nhật thời gian
                    string newVerificationCode = GenerateVerificationCode(); // Tạo mã xác thực mới
                    DateTime newExpirationTime = DateTime.UtcNow.AddMinutes(5); // Thời gian mới

                    // Cập nhật vào DB
                    existingRecord.verificationCode = newVerificationCode; // Cập nhật mã xác thực mới
                    existingRecord.expirationTime = newExpirationTime; // Cập nhật thời gian hết hạn mới

                    await _registerAuthCollection.ReplaceOneAsync(
                        r => r.email == email,
                        existingRecord
                    );

                    // Gửi mã xác thực mới qua email
                    SendVerificationEmail(email, newVerificationCode);

                    return true; // Đã cập nhật mã xác thực thành công
                }

                return false; // Nếu mã xác thực vẫn còn hiệu lực
            }

            return false; // Không tìm thấy email
        }


        //==================
        //lưu mã xác thực vào cơ sở dữ liệu
        public async Task Create(string email, string verificationCode)
        {
            // Kiểm tra null hoặc chuỗi rỗng
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentNullException(nameof(email), "Email không được để trống.");
            }

            if (string.IsNullOrEmpty(verificationCode))
            {
                throw new ArgumentNullException(nameof(verificationCode), "Mã xác thực không được để trống.");
            }

            // Tạo đối tượng RegisterAuth
            var registerAuth = new RegisterAuth
            {
                email = email,
                verificationCode = verificationCode,
                expirationTime = DateTime.UtcNow.AddMinutes(5), // Mã xác thực sẽ hết hạn sau 5 phút
                isVerified = false
            };

            try
            {
                // Chèn dữ liệu vào collection
                await _registerAuthCollection.InsertOneAsync(registerAuth);
            }
            catch (Exception ex)
            {
                // Xử lý lỗi (ghi log, ném ngoại lệ, v.v.)
                throw new Exception("Có lỗi xảy ra trong quá trình chèn dữ liệu vào cơ sở dữ liệu: " + ex.Message);
            }
        }

        //==================
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

