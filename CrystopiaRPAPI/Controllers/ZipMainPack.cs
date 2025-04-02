using Microsoft.AspNetCore.Mvc;
using MoonlightSpaceAPI.Services;
using Renci.SshNet;

namespace CrystopiaRPAPI.Controllers;

[ApiController]
[Route("/copyToProduction")]
public class ZipMainPack : ControllerBase
{
    public ZipMainPack()
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
                var packserver = config.PackServer.First().Value;
                string host = packserver.Host;
                string username = packserver.User;
                string password = packserver.Password;

                using (var sshclient = new SshClient(host, username, password))
                {
                    sshclient.Connect();
                    var command2 =
                        sshclient.CreateCommand(
                            $"docker exec {packserver.Name.ToLower()} mc-send-to-console iaz");
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