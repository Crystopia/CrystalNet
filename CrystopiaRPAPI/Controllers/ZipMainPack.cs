using Microsoft.AspNetCore.Mvc;
using MoonlightSpaceAPI.Services;
using Renci.SshNet;

namespace CrystopiaRPAPI.Controllers;

[ApiController]
[Route("/zipMainPack")]
public class ZipMainPack : ControllerBase
{
    public ZipMainPack()
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var configService = new ConfigService();
        var config = configService.Get();
        var request = Request;

        var headers = request.Headers;

        if (headers.ContainsKey("Authorization"))
        {
            var token = headers["Authorization"].First();

            if (token == config.APIKey)
            {
                var packserver = config.PackServer.First().Value;
                string host = packserver.Host;
                string username = packserver.User;
                string password = packserver.Password;

                using (var sshclient = new SshClient(host, username, password))
                {
                    sshclient.Connect();
                    var command2 =
                        sshclient.CreateCommand(
                            $"docker exec {packserver.Name.ToLower()} mc-send-to-console nexo reload pack");
                    command2.Execute();
                    sshclient.Disconnect();
                }

                Ok(
                    new
                    {
                        success = true,
                        message = "MainPack zipped",
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