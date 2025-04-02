using CrystopiaRPAPI.Models;
using Microsoft.AspNetCore.Mvc;
using MoonlightSpaceAPI.Services;
using Newtonsoft.Json;
using Renci.SshNet;

namespace CrystopiaRPAPI.Controllers;

[ApiController]
[Route("/copyToProduction")]
public class CopyToProduction : ControllerBase
{
    public CopyToProduction()
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var configService = new ConfigService();
        var config = configService.Get();

        var headers = Request.Headers;

        if (headers.ContainsKey("Authorization"))
        {
            var token = headers["Authorization"].First();

            if (token == config.APIKey)
            {
                var serverzip = config.DevServerPluginZipURL;
                var packserver = config.PackServer.First().Value;

                string host = packserver.Host;
                string username = packserver.User;
                string password = packserver.Password;

                using (var sshclient = new SshClient(host, username, password))
                {
                    sshclient.ConnectionInfo.Timeout = TimeSpan.FromSeconds(30);
                    sshclient.Connect();

                    var removeOldFolder =
                        sshclient.CreateCommand($"rm -r /crystopia/{packserver.Name}/plugins/Nexo");
                    removeOldFolder.Execute();

                    var createNewFolder =
                        sshclient.CreateCommand($"mkdir -p /crystopia/{packserver.Name}/plugins/Nexo");
                    createNewFolder.Execute();
                    Console.WriteLine("Nexo Ordner neu erstellt");

                    string sshremotePath = $"/crystopia/{packserver.Name}/plugins/Nexo/pluginzip.zip";
                    string extractPath = $"/crystopia/{packserver.Name}/plugins/Nexo/";

                    var downloadZip = sshclient.CreateCommand($"curl -o {sshremotePath} {serverzip}");
                    downloadZip.Execute();
                    Console.WriteLine("server.zip heruntergeladen");

                    var unzipAndRemove =
                        sshclient.CreateCommand($"unzip -o {sshremotePath} -d {extractPath} && rm {sshremotePath}");
                    unzipAndRemove.Execute();
                    Console.WriteLine("server.zip entpackt und gelöscht");

                    var setPermissions = sshclient.CreateCommand($"chmod -R 777 {extractPath}");
                    setPermissions.Execute();
                    Console.WriteLine("Berechtigungen gesetzt");

                    var dockerCommand =
                        sshclient.CreateCommand($"docker exec {packserver.Name.ToLower()} mc-send-to-console iaz");
                    dockerCommand.Execute();
                    Console.WriteLine("Docker-Befehl ausgeführt");

                    sshclient.Disconnect();
                }


                Console.WriteLine(".zip unzipped - pluginzip.zip deleted - zip Resourcepack");
                return Ok(
                    new
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