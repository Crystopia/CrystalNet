using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CrystopiaRPAPI.Helpers;
using CrystopiaRPAPI.Models;
using Microsoft.OpenApi.Models;
using MoonlightSpaceAPI.Services;
using Newtonsoft.Json;
using Octokit;
using Renci.SshNet;
using JsonSerializer = System.Text.Json.JsonSerializer;
using ProductHeaderValue = System.Net.Http.Headers.ProductHeaderValue;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(
    c => { c.SwaggerDoc("v1", new OpenApiInfo() { Title = "CrystopiaCloudAPI", Version = "v1" }); });

builder.Services.AddScoped<ConfigService>();

builder.Services.AddControllers();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();
app.Run("http://0.0.0.0:5000");