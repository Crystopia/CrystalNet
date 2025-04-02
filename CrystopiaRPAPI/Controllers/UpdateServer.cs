using CrystopiaRPAPI.Models;
using Microsoft.AspNetCore.Mvc;
using MoonlightSpaceAPI.Services;
using Renci.SshNet;

namespace CrystopiaRPAPI.Controllers;

[ApiController]
[Route("/updateServer")]
public class UpdateServer : ControllerBase
{
    public UpdateServer()
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

                    Ok(new
                    {
                        success = true,
                        message = "Server updated",
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