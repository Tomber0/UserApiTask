using Microsoft.EntityFrameworkCore;
using NLog.Extensions.Logging;
using UserApiTask.Configurations;
using UserApiTask.Models;


IConfigurationRoot configuration = new ConfigurationBuilder().SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IConfigurationRoot>(configuration);
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options=>options.UseSqlServer(configuration.GetConnectionString("LocalConnection")));
builder.Services.Configure<UserConfiguration>(configuration.GetSection("PageSize"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging(logging => 
{
    logging.AddConfiguration(configuration.GetSection("Logging"));
    logging.AddEventSourceLogger();
    logging.AddDebug();
    logging.AddConsole();
    logging.AddNLog(); 
});
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
