using System.Text.Json;
using CrystopiaRPAPI.Models;

namespace MoonlightSpaceAPI.Services;

public class ConfigService
{
    private AppConfiguration config { get; set; }

    public ConfigService()
    {
        CreateConfig().Wait();
    }

    private async Task CreateConfig()
    {
        var configFile = File.Exists("config.json");
        if (!configFile)
        {
            var defaultConfig = JsonSerializer.Serialize(new AppConfiguration()
            {
                Nodes = [],
                Port = "",
                DevServer = { },
                PackServer = { },
                GitHubToken = "",
                APIKey = "",
                ServerURL = "",
                DevServerPluginZipURL = "",
                PackServerPluginZipURL = ""
            }, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
            });
            await File.WriteAllTextAsync("config.json", defaultConfig);
        }

        var jsontext = await File.ReadAllTextAsync("config.json");
        config = JsonSerializer.Deserialize<AppConfiguration>(jsontext)!;
    }

    public AppConfiguration Get()
    {
        return config;
    }
}