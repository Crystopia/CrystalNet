using Microsoft.AspNetCore.Mvc;
using MoonlightSpaceAPI.Services;
using Renci.SshNet;

namespace CrystopiaRPAPI.Controllers;

[ApiController]
[Route("/copyToProduction")]
public class ZipDevPack : ControllerBase
{
    public ZipDevPack()
    {
    }

    [HttpGet]
    public async Task Get()
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
                var devserver = config.DevServer.First().Value;
                string host = devserver.Host;
                string username = devserver.User;
                string password = devserver.Password;

                using (var sshclient = new SshClient(host, username, password))
                {
                    sshclient.Connect();

                    var command2 =
                        sshclient.CreateCommand(
                            $"docker exec {devserver.Name.ToLower()} mc-send-to-console nexo reload pack");
                    command2.Execute();
                    sshclient.Disconnect();
                }
            }
            else
            {
                Response.StatusCode = 401;
                Console.WriteLine("Not authorized");
            }
        }
        else
        {
            Response.StatusCode = 401;
            Console.WriteLine("Not authorized - No Authorization");
        }
    }
}