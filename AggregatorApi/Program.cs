using AggregatorApi.Clients;
using AggregatorApi.Clients.OpenMeteo;
using AggregatorApi.Extension;
using AggregatorApi.Middleware;
using AggregatorApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<IAggregationService, AggregationService>();
builder.Services.AddSingleton<IApiStatisticsService, ApiStatisticsService>();

builder.Services.AddHttpClient<IApiClient, OpenMeteoClient>(client => client.BaseAddress = new Uri("https://historical-forecast-api.open-meteo.com/"))
    .AddApiClientResilience(OpenMeteoClient.ClientName);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
