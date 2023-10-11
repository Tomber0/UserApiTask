using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using NLog.Extensions.Logging;
using System.Reflection;
using UserApiTask.Configurations;
using UserApiTask.Models;


IConfigurationRoot configuration = new ConfigurationBuilder().SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IConfigurationRoot>(configuration);
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options=>options.UseSqlServer(configuration.GetConnectionString("LocalConnection")));
builder.Services.Configure<UserConfiguration>(configuration.GetSection("UserOptions"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c=> 
{
    c.SwaggerDoc("v1",new OpenApiInfo 
    {
        Version = "v1",
        Title = "Task API",
        Description = "User Task API"
    });
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

});
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
