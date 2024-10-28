namespace asp.Helper
{
    public class SendMailConstant
    {
        public static string hostEmail = "smtp.gmail.com";
        public static int portEmail = 587;
        public static string emailSender = "quyetbuiqui@gmail.com";
        public static string passwordSender = "pvfl ljkw ckie onau";
        public static string bodyEmail(string verificationCode)
        {
            return $@"
                    <html>
                    <head>
                        <style>
                            body {{
                                font-family: Arial, sans-serif;
                                background-color: #f4f4f4;
                                padding: 20px;
                            }}
                            .container {{
                                max-width: 600px;
                                margin: auto;
                                background: #ffffff;
                                padding: 20px;
                                border-radius: 8px;
                                box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
                            }}
                            h2 {{
                                text-transform: uppercase;
                                color: black
                            }}
                            h3 {{
                                font-weight: bold;
                                color: #2196F3;
                                font-size: 30px;
                                letter-spacing: 3px; 
                            }}
                            p {{
                                color: #333;
                            }}
                            footer {{
                                margin-top: 20px;
                                font-size: 12px;
                                color: #999999;
                            }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <h2>Xin chào!</h2>
                            <p>Cảm ơn bạn đã đăng ký. Đây là mã xác thực của bạn:</p>
                            <h3>{verificationCode}</h3>
                            <p>Vui lòng nhập mã này để hoàn tất quá trình xác thực.</p>
                            <p>Để đảm bảo an toàn, hãy không chia sẻ mã này với bất kỳ ai.</p>
                            <p>Nếu bạn không yêu cầu xác thực, vui lòng bỏ qua email này.</p>
                            <footer>
                                <p>Trân trọng,<br />Đội ngũ hỗ trợ</p>
                            </footer>
                        </div>
                    </body>
                    </html>";
        }
    }
}
