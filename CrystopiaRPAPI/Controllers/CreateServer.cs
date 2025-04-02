using CrystopiaRPAPI.Models;
using Microsoft.AspNetCore.Mvc;
using MoonlightSpaceAPI.Services;
using Octokit;
using Renci.SshNet;

namespace CrystopiaRPAPI.Controllers;

[ApiController]
[Route("/createServer")]
public class CreateServer : Controller
{
    public CreateServer()
    {
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CloudServer cloudServer)
    {
        var request = Request;

        var configService = new ConfigService();
        var config = configService.Get();
        var headers = request.Headers;

        if (headers.ContainsKey("Authorization"))
        {
            var token = headers["Authorization"].First();

            if (token == config.APIKey)
            {
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

                    return Ok(new
                    {
                        success = true,
                        message = "Server created",
                    });
                }
            }
            else
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Unauthorized",
                });
            }
        }
        else
        {
            return Unauthorized(new
            {
                success = false,
                message = "Unauthorized",
            });
        }

        return Unauthorized(new
        {
            success = false,
            message = "Unauthorized",
        });
    }
}