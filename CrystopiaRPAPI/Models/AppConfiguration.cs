namespace CrystopiaRPAPI.Models;

public class AppConfiguration
{
    public string APIKey { get; set; }
    public string Port { get; set; }

    public string DevServerPluginZipURL { get; set; }
    public string PackServerPluginZipURL { get; set; }

    public string ServerURL { get; set; }

    public Dictionary<string, NodeInfo> Nodes { get; set; } = new Dictionary<string, NodeInfo>();

    public Dictionary<string, ServerModel> DevServer { get; set; } = new Dictionary<string, ServerModel>();

    public Dictionary<string, ServerModel> PackServer { get; set; } = new Dictionary<string, ServerModel>();
}