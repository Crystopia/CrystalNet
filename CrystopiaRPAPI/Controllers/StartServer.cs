using CrystopiaRPAPI.Models;
using Microsoft.AspNetCore.Mvc;
using MoonlightSpaceAPI.Services;
using Renci.SshNet;

namespace CrystopiaRPAPI.Controllers;

[ApiController]
[Route("/startServer")]
public class StartServer : Controller
{
    public StartServer()
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
                            $"docker start {serverAction.Name.ToLower()}");
                    await command2.ExecuteAsync();
                    sshclient.Disconnect();
                }

                return Ok(
                    new
                    {
                        success = true,
                        message = "Server started",
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
    }
}