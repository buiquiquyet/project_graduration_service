using asp.Models;
using asp.Respositories;
using asp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<MongoDbSetting>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<DepartmentService>();
builder.Services.AddSingleton<ClassService>();
builder.Services.AddSingleton<RecordService>();
builder.Services.AddSingleton<SemesterService>();
builder.Services.AddSingleton<FileService>();
builder.Services.AddSingleton<InstructorService>();
builder.Services.AddSingleton<SubjectService>();
//JWT
//var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);
string key = KeyGenerator.Generate256BitKey();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});
builder.Services.AddAuthorization();
builder.Services.AddSingleton<JWTService>(new JWTService(builder.Configuration["Jwt:Issuer"], builder.Configuration["Jwt:Audience"], builder.Configuration["Jwt:Key"]));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("GVCN", policy => policy.RequireRole("GVCN"));
    options.AddPolicy("TBM", policy => policy.RequireRole("TBM"));
    options.AddPolicy("ADMIN", policy => policy.RequireRole("ADMIN"));
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1000000000; // Giới hạn kích thước tệp đính kèm lên (1GB)
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 1000000000; // Giới hạn kích thước request body (1GB)
});

builder.Services.AddControllers()
    .AddJsonOptions(
        options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
var MyAllowOrigins = "AllowAll";
builder.Services.AddCors(options =>
{
    options.AddPolicy(MyAllowOrigins,
        builder => builder.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod());
});
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



var app = builder.Build();

app.UseCors(MyAllowOrigins);
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//file
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
           Path.Combine(builder.Environment.ContentRootPath, "Files")),
    RequestPath = "/StaticFiles"
});
//JWT
app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
