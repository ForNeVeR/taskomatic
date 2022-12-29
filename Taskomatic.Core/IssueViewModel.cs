using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Octokit;
using ReactiveUI;

namespace Taskomatic.Core;

public class IssueViewModel
{
    public string Project { get; }
    public int Id { get; }
    public string Name { get; }

    public ItemState Status { get; }
    public IReadOnlyList<string> Assignees { get; }

    public string FullInfo => $"{Project}#{Id}: {Name}";

    public string AssigneeNames => string.Join(",", Assignees);

    public LazyAsync<string> LocalStatus { get; }

    public ReactiveCommand<Unit, Unit> SyncCommand { get; }

    public IssueViewModel(Config config, string project, Issue issue)
    {
        Project = project;
        Id = issue.Number;
        Name = issue.Title;
        Status = issue.State;
        Assignees = issue.Assignees.Select(u => u.Login).ToList();

        LocalStatus = new LazyAsync<string>(() => GetLocalStatus(config, project, Id), "Loading…");
        SyncCommand = ReactiveCommand.CreateFromTask(
            async _ =>
            {
                await SyncTask(config, project, Id, Name);
                LocalStatus.Reset();
                var value = LocalStatus.Value;
            },
            LocalStatus.ObservableForProperty(ls => ls.Value).Select(p => p.Value == "Not imported"));
    }

    private static string EscapeProjectName(string name) => ArgumentProcessor.CygwinPrepareArgument(name);

    private class TaskwarriorTaskInfo { }

    private Task<string> GetLocalStatus(Config config, string project, int id)
    {
        var simpleProjectName = project.Split('/')[1];
        var startInfo = StartTaskWarrior(
            config,
            $"taskomatic_ghproject:{EscapeProjectName(project)}",
            $"taskomatic_id:{id}",
            "or",
            $"/{simpleProjectName}#{id}: /",
            "export");
        return Task.Run(async () =>
        {
            using var process = Process.Start(startInfo);
            var output = await GetExecutionResult(process);
            if (output.ExitCode != 0)
                throw new Exception($"Process has exited with code {output.ExitCode}:\n{output.StdErr}");

            var list = JsonConvert.DeserializeObject<List<TaskwarriorTaskInfo>>(output.StdOut);
            return list.Count == 0 ? "Not imported" : "Imported";
        });
    }

    private struct ProcessExecutionResult
    {
        public string StdOut, StdErr;
        public int ExitCode;
    }

    private static async Task<ProcessExecutionResult> GetExecutionResult(Process process)
    {
        var stdOut = process.StandardOutput.ReadToEndAsync();
        var stdErr = process.StandardError.ReadToEndAsync();
        await Task.Run(process.WaitForExit);
        return new ProcessExecutionResult
        {
            StdOut = await stdOut,
            StdErr = await stdErr,
            ExitCode = process.ExitCode
        };
    }

    private async Task SyncTask(Config config, string project, int id, string name)
    {
        var status = await GetLocalStatus(config, project, id);
        if (status != "Not imported")
        {
            return;
        }

        var startInfo = StartTaskWarrior(
            config,
            "add",
            $"{project}#{id}: {name}",
            $"taskomatic_ghproject:{EscapeProjectName(project)}",
            $"taskomatic_id:{id}");

        await Task.Run(() =>
        {
            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new Exception("TaskWarrior process returned error exit code: " + process.ExitCode);
                }
            }
        });
    }

    private ProcessStartInfo StartTaskWarrior(Config config, params string[] args)
    {
        var executablePath = (config.TaskWarriorPath, config.TaskWarriorCommand) switch
        {
            ({ } path, null) => path,
            (null,  [var executable, ..]) => executable,
            _ => throw new Exception($"Invalid configuration: only one of {nameof(config.TaskWarriorPath)} " +
                                     $"or {config.TaskWarriorCommand} should be defined, and " +
                                     $"{nameof(config.TaskWarriorCommand)} should include at least one item.")
        };

        var arguments = new List<string>();
        if (config.TaskWarriorCommand != null)
            arguments.AddRange(config.TaskWarriorCommand.Skip(1));
        arguments.AddRange(args);

        return new ProcessStartInfo(
            executablePath,
            ArgumentProcessor.CygwinArgumentsToString(arguments))
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
    }
}
