using CrystopiaRPAPI.Models;
using Microsoft.AspNetCore.Mvc;
using MoonlightSpaceAPI.Services;
using Renci.SshNet;

namespace CrystopiaRPAPI.Controllers;

[ApiController]
[Route("/stopServer")]
public class StopServer : Controller
{
    public StopServer()
    {
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ServerAction serverAction)
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

                    Ok(
                        new
                        {
                            success = true,
                            message = "Server stopped",
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