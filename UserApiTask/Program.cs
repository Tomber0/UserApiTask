using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UserApiTask.Configurations;
using UserApiTask.Models;

var builder = WebApplication.CreateBuilder(args);

IConfigurationRoot configuration = new ConfigurationBuilder().SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

builder.Services.AddSingleton<IConfigurationRoot>(configuration);
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options=>options.UseSqlServer(configuration.GetConnectionString("LocalConnection")));

builder.Services.Configure<UserConfiguration>(configuration.GetSection("PageSize"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
