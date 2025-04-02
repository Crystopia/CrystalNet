using CrystopiaRPAPI.Models;
using Microsoft.AspNetCore.Mvc;
using MoonlightSpaceAPI.Services;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Iana;
using Renci.SshNet;

namespace CrystopiaRPAPI.Controllers;

[ApiController]
[Route("/applyToOtherServer")]
public class ApplyToOtherServer : Controller
{
    public ApplyToOtherServer()
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get()
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
                string url = config.ServerURL;

                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(url);
                response
                    .EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();

                // Default Variabels
                string fileUrl = config.PackServerPluginZipURL;
                try
                {
                    List<ServerInfo> servers = JsonConvert.DeserializeObject<List<ServerInfo>>(responseBody);


                    foreach (var server in servers)
                    {
                        var node = config.Nodes[server.Ip];
                        if (node == null) continue;


                        string host = server.Ip;
                        string username = node.User;
                        string password = node.Password;

                        using (var sshclient = new SshClient(host, username, password))
                        {
                            sshclient.Connect();
                            var command =
                                sshclient.CreateCommand($"rm -r /crystopia/{server.Name}/plugins/Nexo");
                            command.Execute();
                            var command2 =
                                sshclient.CreateCommand($"cd /crystopia/{server.Name}/plugins/ && mkdir Nexo");
                            command2.Execute();
                            sshclient.Disconnect();
                        }

                        Console.WriteLine("Nexo cleared");

                        string sftpHost = server.Ip;
                        int sftpPort = 22;
                        string sftpUser = node.User;
                        string sftpPass = node.Password;
                        string remotePath = $"/crystopia/{server.Name}/plugins/Nexo/";
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

                        string sshremotePath = $"/crystopia/{server.Name}/plugins/Nexo/pluginzip.zip";
                        string extractPath = $"/crystopia/{server.Name}/plugins/Nexo/";

                        using (var sshclient = new SshClient(host, username, password))
                        {
                            sshclient.Connect();
                            var command =
                                sshclient.CreateCommand(
                                    $"unzip -o {sshremotePath} -d {extractPath} && rm {sshremotePath}");
                            command.Execute();

                            var command2 =
                                sshclient.CreateCommand(
                                    $"docker exec {server.Name.ToLower()} mc-send-to-console nexo reload all");
                            command2.Execute();
                            sshclient.Disconnect();
                        }

                        Console.WriteLine(".zip unzipped - pluginzip.zip deleted");
                        return Ok(new
                        {
                            success = true,
                            message = "Plugin updated",
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex}");
                }

                Response.StatusCode = 200;
                Response.ContentType = "application/json";
                return Ok(new
                {
                    success = true,
                    message = "Plugin updated",
                });
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

        return Unauthorized(new
        {
            success = false,
            message = "Unauthorized",
        });
    }
}