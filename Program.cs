using FactoryVisitorApp.Data;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ✅ CORS configuration for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy => policy.WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

// ✅ Swagger configuration
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Factory Visitor API",
        Version = "v1",
        Description = "API for managing factory visitors, zones, and user authentication"
    });
});

var app = builder.Build();

// ✅ Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Factory Visitor API v1");
    });
}

app.UseHttpsRedirection();


app.UseCors("AllowReactApp");

app.UseAuthorization();

app.MapControllers();

app.Run();
