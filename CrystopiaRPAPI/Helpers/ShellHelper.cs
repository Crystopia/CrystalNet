using System.Diagnostics;
using System.Text;

namespace CrystopiaRPAPI.Helpers;

public static class ShellHelper
{
    public static async Task<string> ExecuteCommand(string command, bool ignoreErrors = false)
    {
        Process process = new Process();


        process.StartInfo.FileName = "/bin/sh";
        process.StartInfo.Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            if (!ignoreErrors)
                throw new Exception(await process.StandardError.ReadToEndAsync());
        }

        return output;
    }

    public static async Task<string> ExecuteCommandRaw(string command)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                outputBuilder.AppendLine(args.Data);
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                errorBuilder.AppendLine(args.Data);
        };

        process.Start();

        // Begin reading the output and error streams
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait for the process to finish
        await process.WaitForExitAsync();

        // Combine both output and error streams into one result
        string result = outputBuilder.ToString();
        string error = errorBuilder.ToString();

        return string.IsNullOrWhiteSpace(error) ? result : $"{result}\nError: {error}";
    }
}