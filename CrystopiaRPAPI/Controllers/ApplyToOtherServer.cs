using CrystopiaRPAPI.Models;
using Microsoft.AspNetCore.Mvc;
using MoonlightSpaceAPI.Services;
using Newtonsoft.Json;
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
        var configService = new ConfigService();
        var config = configService.Get();

        var headers = Request.Headers["Authorization"];
        var token = headers.FirstOrDefault();

        if (token == config.APIKey)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(config.ServerURL);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    List<ServerInfo> servers = JsonConvert.DeserializeObject<List<ServerInfo>>(responseBody);
                    if (servers == null || servers.Count == 0)
                        return BadRequest(new { success = false, message = "Keine Server gefunden." });

                    string fileUrl = config.PackServerPluginZipURL;
                    List<object> results = new List<object>();

                    foreach (var server in servers)
                    {
                        try
                        {
                            Console.WriteLine($"Verarbeite Server: {server.Name}");

                            var node = config.Nodes.GetValueOrDefault(server.Ip);
                            if (node == null)
                            {
                                Console.WriteLine($"Kein Knoten für {server.Ip} gefunden, überspringe...");
                                continue;
                            }

                            string host = server.Ip;
                            string username = node.User;
                            string password = node.Password;
                            string pluginPath = $"/crystopia/{server.Name}/plugins/Nexo";

                            using (var sshclient = new SshClient(host, username, password))
                            {
                                sshclient.ConnectionInfo.Timeout = TimeSpan.FromSeconds(30);
                                sshclient.Connect();

                                sshclient.CreateCommand($"rm -r {pluginPath}").Execute();
                                sshclient.CreateCommand($"mkdir -p {pluginPath}").Execute();
                                Console.WriteLine("Nexo-Ordner geleert und neu erstellt");

                                string sshremotePath = $"{pluginPath}/pluginzip.zip";
                                sshclient.CreateCommand($"curl -o {sshremotePath} {fileUrl}").Execute();
                                Console.WriteLine("server.zip heruntergeladen");

                                sshclient.CreateCommand(
                                    $"unzip -o {sshremotePath} -d {pluginPath} && rm {sshremotePath}").Execute();
                                Console.WriteLine("server.zip entpackt und gelöscht");

                                sshclient.CreateCommand($"chmod -R 777 {pluginPath}").Execute();
                                Console.WriteLine("Berechtigungen gesetzt");

                                sshclient.CreateCommand(
                                        $"docker exec {server.Name.ToLower()} mc-send-to-console nexo reload all")
                                    .Execute();
                                Console.WriteLine("Docker-Befehl ausgeführt");

                                sshclient.Disconnect();
                            }

                            results.Add(new { server = server.Name, success = true, message = "Plugin updated" });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Fehler bei {server.Name}: {ex.Message}");
                            results.Add(
                                new { server = server.Name, success = false, message = $"Fehler: {ex.Message}" });
                        }
                    }

                    return Ok(new { success = true, message = "Updates abgeschlossen", results });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Serverfehler: {ex.Message}" });
            }
        }

        return Unauthorized(new { success = false, message = "Unauthorized" });
    }
}