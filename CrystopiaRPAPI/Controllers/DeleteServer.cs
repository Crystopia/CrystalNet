using CrystopiaRPAPI.Models;
using Microsoft.AspNetCore.Mvc;
using MoonlightSpaceAPI.Services;
using Renci.SshNet;

namespace CrystopiaRPAPI.Controllers;

[ApiController]
[Route("/deleteServer")]
public class DeleteServer : ControllerBase
{
    public DeleteServer()
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

            if (!serverAction.Action.Equals("Force"))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Unauthorized",
                });
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

                    return Ok(
                        new
                        {
                            success = true,
                            message = "Server deleted",
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