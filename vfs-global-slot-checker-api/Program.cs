var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<vfs_global_slot_checker_api.Models.VfsOptions>(builder.Configuration.GetSection("Vfs"));
builder.Services.AddSingleton<vfs_global_slot_checker_api.Services.VfsSessionState>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient<vfs_global_slot_checker_api.Services.VfsAvailabilityClient>(client =>
{
    client.BaseAddress = new Uri("https://lift-api.vfsglobal.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
