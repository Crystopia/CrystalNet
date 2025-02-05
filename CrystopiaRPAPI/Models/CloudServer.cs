namespace CrystopiaRPAPI.Models;

public class CloudServer
{
    public string Name { get; set; }
    public string ServerOptions { get; set; }
    public int Port { get; set; }
    public string Host { get; set; }
    public string TemplateURL { get; set; }
    public string ItemsAdderPluginZipURL { get; set; }
}