using asp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace asp.Services
{
    public class JWTService
    {
        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _key;

        public JWTService(string issuer, string audience, string key)
        {
            _issuer = issuer;
            _audience = audience;
            _key = key;
        }

        public string GenerateToken(string userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_key);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("ID", userId)

                }),
                Expires = DateTime.UtcNow.AddHours(12),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        public string DecodeToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_key);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true 
            };

            try
            {
                SecurityToken validatedToken;
                var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;

                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    return "";
                }

                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "ID");
                var userId = userIdClaim?.Value;

                return userId;
            }
            catch (Exception ex)
            {
                // Xử lý các ngoại lệ nếu cần
                return "";
            }
        }



    }
}
