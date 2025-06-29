using AggregatorApi.Clients;
using AggregatorApi.Clients.OpenMeteo;
using AggregatorApi.Clients.NewsApi;
using AggregatorApi.Clients.NobelPrize;
using AggregatorApi.Configuration;
using AggregatorApi.Extension;
using AggregatorApi.Middleware;
using AggregatorApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAggregationService, AggregationService>();
builder.Services.AddSingleton<IApiStatisticsService, ApiStatisticsService>();

builder.Services.AddHybridCache();

builder.Services.AddHttpClient<OpenMeteoClient>(client => client.BaseAddress = new Uri("https://historical-forecast-api.open-meteo.com/"))
    .AddApiClientResilience(OpenMeteoClient.ClientName);
builder.Services.AddHttpClient<NewsApiClient>(client =>
{
    client.BaseAddress = new Uri("https://newsapi.org/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AggregatorApi/1.0");
}).AddApiClientResilience(NewsApiClient.ClientName);
builder.Services.AddHttpClient<NobelPrizeClient>(client => client.BaseAddress = new Uri("http://api.nobelprize.org/"))
.AddApiClientResilience(NobelPrizeClient.ClientName);

builder.Services.AddTransient<IApiClient>(sp => sp.GetRequiredService<OpenMeteoClient>());
builder.Services.AddTransient<IApiClient>(sp => sp.GetRequiredService<NewsApiClient>());
builder.Services.AddTransient<IApiClient>(sp => sp.GetRequiredService<NobelPrizeClient>());

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret!))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
