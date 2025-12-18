using SemestralAPI.Services;

namespace SemestralAPI {
  public class Program {
    public static void Main(string[] args) {
      var builder = WebApplication.CreateBuilder(args);

      // Controllers
      builder.Services.AddControllers();

      // Swagger
      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddSwaggerGen();

      // Services
      builder.Services.AddSingleton<AuthService>();

      // CORS
      builder.Services.AddCors(options =>
      {
        options.AddPolicy("AllowFrontend", policy =>
        {
          policy
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin();
        });
      });

      var app = builder.Build();

      if (app.Environment.IsDevelopment()) {
        app.UseSwagger();
        app.UseSwaggerUI();
      }

      // CORS middleware
      app.UseCors("AllowFrontend");

      app.UseHttpsRedirection();

      app.UseAuthorization();

      app.MapControllers();

      app.Run();
    }
  }
}
