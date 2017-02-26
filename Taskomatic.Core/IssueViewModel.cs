﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Octokit;

namespace Taskomatic.Core
{
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

        public IssueViewModel(Config config, Issue issue)
        {
            Project = config.GitHubProject;
            Id = issue.Number;
            Name = issue.Title;
            Status = issue.State;
            Assignees = issue.Assignees.Select(u => u.Login).ToList();
            
            LocalStatus = new LazyAsync<string>(() => GetLocalStatus(config, Id), "Loading…");
        }

        private class TaskwarriorTaskInfo { }

        private Task<string> GetLocalStatus(Config config, int id)
        {
            var project = config.GitHubProject;
            var startInfo = new ProcessStartInfo(
                config.TaskWarriorPath,
                $"taskomatic_ghproject:{project} taskomatic_id:{id} export")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            return Task.Run(() =>
            {
                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    var serializer = new JsonSerializer();
                    using (var streamReader = process.StandardOutput)
                    using (var reader = new JsonTextReader(streamReader))
                    {
                        var list = serializer.Deserialize<List<TaskwarriorTaskInfo>>(reader);
                        return list.Count == 0 ? "Not imported" : "Imported";
                    }
                }
            });
        }
    }
}
