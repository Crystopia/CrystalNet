using System.Net.Http.Headers;
using System.Text.Json;
using CrystopiaRPAPI.Helpers;
using CrystopiaRPAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var jsontext = await File.ReadAllTextAsync("config.json");
var config = JsonSerializer.Deserialize<AppConfiguration>(jsontext)!;

app.MapPost("/api/pack/unzip", async (HttpContext context) =>
    {
        var request = context.Request;

        var headers = request.Headers;

        if (headers.ContainsKey("Authorization"))
        {
            var token = headers["Authorization"].First();

            if (token == config.APIKey)
            {
                // Change this 
                await ShellHelper.ExecuteCommand("crystopia pack unzip");
                context.Response.StatusCode = 200;
                Console.WriteLine("Running crystopia pack unzip");
            }
            else
            {
                context.Response.StatusCode = 401;
                Console.WriteLine("Not authorized");
            }
        }
        else
        {
            context.Response.StatusCode = 401;
            Console.WriteLine("Not authorized - No Authorization");
        }
    })
    .WithName("Unzip Files from Resourcepack")
    .WithOpenApi();

app.MapPost("/api/pack/packsquash", async (HttpContext context) =>
    {
        var request = context.Request;

        var headers = request.Headers;

        if (headers.ContainsKey("Authorization"))
        {
            var token = headers["Authorization"].First();

            if (token == config.APIKey)
            {
                // Change this
                var command = await ShellHelper.ExecuteCommandRaw("crystopia pack packsquash");
                Console.WriteLine(command);
                context.Response.StatusCode = 200;
                Console.WriteLine("Running crystopia pack packsquash");
            }
            else
            {
                context.Response.StatusCode = 401;
                Console.WriteLine("Not authorized");
            }
        }
        else
        {
            context.Response.StatusCode = 401;
            Console.WriteLine("Not authorized - No Authorization");
        }
    })
    .WithName("Encrypt the Resourcepack")
    .WithOpenApi();

app.MapGet("/api/pack/apply",
        async (HttpContext context) =>
        {
            return Results.File(await File.ReadAllBytesAsync(config.ResourcepackFile), "application/octet-stream", "generated.zip");
        })
    .WithName("Apply the resourcepack")
    .WithOpenApi();

app.MapGet("/api/pack/dev/apply",
        async (HttpContext context) =>
        {
            return Results.File(await File.ReadAllBytesAsync(config.DevResourcepackFile), "application/octet-stream", "generated.zip");
        })
    .WithName("Send the Dev Resourcepack")
    .WithOpenApi();

app.MapPost("/api/pack/reset", async (HttpContext context) =>
    {
        var request = context.Request;

        var headers = request.Headers;

        if (headers.ContainsKey("Authorization"))
        {
            var token = headers["Authorization"].First();

            if (token == config.APIKey)
            {
                // Change this 
                await ShellHelper.ExecuteCommand("crystopia pack reset");
                context.Response.StatusCode = 200;
                Console.WriteLine("Running crystopia pack reset");
            }
            else
            {
                context.Response.StatusCode = 401;
                Console.WriteLine("Not authorized");
            }
        }
        else
        {
            context.Response.StatusCode = 401;
            Console.WriteLine("Not authorized - No Authorization");
        }
    })
    .WithName("Reset the Resourcepack Files")
    .WithOpenApi();

app.MapPost("/api/files/copypack", async (HttpContext context) =>
    {
        var request = context.Request;

        var headers = request.Headers;

        if (headers.ContainsKey("Authorization"))
        {
            var token = headers["Authorization"].First();

            if (token == config.APIKey)
            {
                // Change this 
                await ShellHelper.ExecuteCommand("crystopia files copypack");
                context.Response.StatusCode = 200;
                Console.WriteLine("Running crystopia files copypack");
            }
            else
            {
                context.Response.StatusCode = 401;
                Console.WriteLine("Not authorized");
            }
        }
        else
        {
            context.Response.StatusCode = 401;
            Console.WriteLine("Not authorized - No Authorization");
        }
    })
    .WithName("Copy Resourcepack files")
    .WithOpenApi();


app.Run("http://0.0.0.0:" + config.Port);