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



var jsontext = await File.ReadAllTextAsync("config.json");
var config = JsonSerializer.Deserialize<AppConfiguration>(jsontext)!;

app.MapPost("/createServer",
        async (CloudServer cloudServer, HttpContext context) =>
        {
            var request = context.Request;

            var headers = request.Headers;

            if (headers.ContainsKey("Authorization"))
            {
                var token = headers["Authorization"].First();

                if (token == config.APIKey)
                {
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync("SUCCESS");

                    string host = cloudServer.Host;
                    var node = config.Nodes.First().Value;
                    string username = node.User;
                    string password = node.Password;

                    using (var sshclient = new SshClient(host, username, password))
                    {
                        sshclient.Connect();

                        var serverOptions = cloudServer.ServerOptions;

                        var command =
                            sshclient.CreateCommand(
                                $"docker run --name {cloudServer.Name.ToLower()} -d -it {serverOptions.Replace(";", " ")} -e EULA=TRUE -v /crystopia/{cloudServer.Name}/:/data -p {cloudServer.Port}:{cloudServer.Port} itzg/minecraft-server");
                        command.Execute();
                        Console.WriteLine("Created server");


                        var ghClient = new GitHubClient(new Octokit.ProductHeaderValue("CrystopiaCloudWebAPI"));
                        var tokenAuth = new Credentials(config.GitHubToken);
                        ghClient.Credentials = tokenAuth;

                        var releases = await ghClient.Repository.Release.GetAll("Crystopia", "ServerTemplates");
                        var latest = releases[0];

                        foreach (var asset in latest.Assets)
                        {
                            if (asset.Name.Equals(cloudServer.TemplateName))
                            {
                                var assatId = asset.Id;

                                var curlCommand = sshclient.CreateCommand(
                                    $"cd /crystopia/{cloudServer.Name} && curl -L -H \"Accept: application/octet-stream\" -H \"Authorization: Bearer {config.GitHubToken}\"  -H \"X-GitHub-Api-Version: 2022-11-28\"  https://api.github.com/repos/Crystopia/ServerTemplates/releases/assets/{assatId} -o server.zip");
                                curlCommand.Execute();
                            }
                        }

                        Console.WriteLine("Download successful");

                        string sshremotePath = $"/crystopia/{cloudServer.Name}/server.zip";
                        string extractPath = $"/crystopia/{cloudServer.Name}/";

                        var command2 =
                            sshclient.CreateCommand(
                                $"unzip -o {sshremotePath} -d {extractPath}");
                        command2.Execute();
                        Console.WriteLine("Unzipped successful");

                        var rmCommand = sshclient.CreateCommand($"rm -r {sshremotePath}");
                        rmCommand.Execute();
                        Console.WriteLine("Removed successful");

                        sshclient.Disconnect();
                    }

                    string fileUrl = config.PackServerPluginZipURL;

                    using (var sshclient = new SshClient(host, username, password))
                    {
                        sshclient.Connect();
                        var command =
                            sshclient.CreateCommand($"rm -r /crystopia/{cloudServer.Name}/plugins/Nexo");
                        command.Execute();
                        var command2 =
                            sshclient.CreateCommand($"cd /crystopia/{cloudServer.Name}/plugins/ && mkdir Nexo");
                        command2.Execute();
                        sshclient.Disconnect();
                    }

                    Console.WriteLine("Nexo cleared");

                    string sftpHost = cloudServer.Host;
                    int sftpPort = 22;
                    string sftpUser = node.User;
                    string sftpPass = node.Password;
                    string remotePath = $"/crystopia/{cloudServer.Name}/plugins/Nexo/";
                    string remoteFilePath = $"{remotePath}pluginzip.zip";

                    using (HttpClient httpClient = new HttpClient())
                    using (Stream fileStream = await httpClient.GetStreamAsync(fileUrl))
                    using (MemoryStream memoryStream = new MemoryStream())
                    using (SftpClient sftpClient = new SftpClient(sftpHost, sftpPort, sftpUser, sftpPass))
                    {
                        await fileStream.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;

                        sftpClient.Connect();

                        if (!sftpClient.Exists(remotePath))
                        {
                            sftpClient.CreateDirectory(remotePath);
                        }

                        using (Stream sftpStream = sftpClient.OpenWrite(remoteFilePath))
                        {
                            await memoryStream.CopyToAsync(sftpStream);
                        }

                        sftpClient.Disconnect();
                    }

                    Console.WriteLine("pluginzip.zip updated");

                    string pluginsshremotePath = $"/crystopia/{cloudServer.Name}/plugins/Nexo/pluginzip.zip";
                    string pluginextractPath = $"/crystopia/{cloudServer.Name}/plugins/Nexo/";

                    using (var sshclient = new SshClient(host, username, password))
                    {
                        sshclient.Connect();
                        var command =
                            sshclient.CreateCommand(
                                $"unzip -o {pluginsshremotePath} -d {pluginextractPath} && rm {pluginsshremotePath}");
                        command.Execute();
                        Console.WriteLine(".zip unzipped - pluginzip.zip deleted");

                        var command4 = sshclient.CreateCommand($"sudo chmod -R 777 /crystopia/{cloudServer.Name}/");
                        command4.Execute();
                        var command2 = sshclient.CreateCommand($"docker restart {cloudServer.Name.ToLower()}");
                        command2.Execute();

                        Console.WriteLine("Server restarted");

                        sshclient.Disconnect();
                    }
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
    .WithName("createServer")
    .WithOpenApi();

app.MapPost("/startServer",
        async (ServerAction serverAction, HttpContext context) =>
        {
            var request = context.Request;

            var headers = request.Headers;

            if (headers.ContainsKey("Authorization"))
            {
                var token = headers["Authorization"].First();

                if (token == config.APIKey)
                {
                    var node = config.Nodes[serverAction.Host];
                    string host = serverAction.Host;
                    string username = node.User;
                    string password = node.Password;

                    using (var sshclient = new SshClient(host, username, password))
                    {
                        sshclient.Connect();

                        var command2 =
                            sshclient.CreateCommand(
                                $"docker start {serverAction.Name.ToLower()}");
                        await command2.ExecuteAsync();
                        sshclient.Disconnect();
                    }
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
    .WithName("startServer")
    .WithOpenApi();

app.MapPost("/stopServer",
        async (ServerAction serverAction, HttpContext context) =>
        {
            var request = context.Request;

            var headers = request.Headers;

            if (headers.ContainsKey("Authorization"))
            {
                var token = headers["Authorization"].First();

                if (token == config.APIKey)
                {
                    var node = config.Nodes[serverAction.Host];
                    string host = serverAction.Host;
                    string username = node.User;
                    string password = node.Password;

                    using (var sshclient = new SshClient(host, username, password))
                    {
                        sshclient.Connect();

                        var command2 =
                            sshclient.CreateCommand(
                                $"docker stop {serverAction.Name.ToLower()}");
                        await command2.ExecuteAsync();
                        sshclient.Disconnect();
                    }
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
    .WithName("stopServer")
    .WithOpenApi();

app.MapPost("/updateServer",
        async (CloudServer cloudServer, HttpContext context) =>
        {
            var request = context.Request;

            var headers = request.Headers;

            if (headers.ContainsKey("Authorization"))
            {
                var token = headers["Authorization"].First();

                if (token == config.APIKey)
                {
                    var node = config.Nodes[cloudServer.Host];
                    string host = cloudServer.Host;
                    string username = node.User;
                    string password = node.Password;

                    using (var sshclient = new SshClient(host, username, password))
                    {
                        sshclient.Connect();

                        var command2 =
                            sshclient.CreateCommand(
                                $"docker stop {cloudServer.Name.ToLower()}");
                        await command2.ExecuteAsync();

                        var command3 =
                            sshclient.CreateCommand(
                                $"docker rm {cloudServer.Name.ToLower()}");
                        await command3.ExecuteAsync();

                        var serverOptions = cloudServer.ServerOptions;
                        var command = sshclient.CreateCommand(
                            $"docker run --name {cloudServer.Name.ToLower()} -d -it {serverOptions.Replace(";", " ")} -e EULA=TRUE -v /crystopia/{cloudServer.Name}/:/data -p {cloudServer.Port}:{cloudServer.Port} itzg/minecraft-server");
                        await command.ExecuteAsync();

                        sshclient.Disconnect();
                    }
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
    .WithName("updateServer")
    .WithOpenApi();

app.MapPost("/deleteServer",
        async (ServerAction serverAction, HttpContext context) =>
        {
            var request = context.Request;

            var headers = request.Headers;

            if (headers.ContainsKey("Authorization"))
            {
                var token = headers["Authorization"].First();

                if (!serverAction.Action.Equals("Force"))
                {
                    context.Response.StatusCode = 401;
                    return;
                }

                if (token == config.APIKey)
                {
                    var node = config.Nodes[serverAction.Host];
                    string host = serverAction.Host;
                    string username = node.User;
                    string password = node.Password;

                    using (var sshclient = new SshClient(host, username, password))
                    {
                        sshclient.Connect();

                        var command2 =
                            sshclient.CreateCommand(
                                $"docker stop {serverAction.Name.ToLower()}");
                        await command2.ExecuteAsync();

                        var command3 =
                            sshclient.CreateCommand(
                                $"docker rm {serverAction.Name.ToLower()}");
                        await command3.ExecuteAsync();


                        var command = sshclient.CreateCommand(
                            $"rm -r /crystopia/{serverAction.Name.ToLower()}/");
                        await command.ExecuteAsync();

                        sshclient.Disconnect();
                    }
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
    .WithName("deleteServer")
    .WithOpenApi();


app.Run("http://0.0.0.0:" + config.Port);