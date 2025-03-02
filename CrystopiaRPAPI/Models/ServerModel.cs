namespace CrystopiaRPAPI.Models;

public class ServerModel
{
    public string Name { get; set; }
    public string User { get; set; } = "root";
    public string Password { get; set; } = "";
    public string Host { get; set; } = "";
}