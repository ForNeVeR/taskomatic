﻿taskomatic
==========

Taskomatic is a GUI tool to synchronize GitHub issues with a local
[TaskWarrior][taskwarrior] database.

Setup
-----

```console
$ task config uda.taskomatic_ghproject.type string
$ task config uda.taskomatic_ghproject.label "Taskomatic: GitHub project"

$ task config uda.taskomatic_id.type string
$ task config uda.taskomatic_id.label "Taskomatic: GitHub id"
```

Configuration
-------------

Put this into `~/.taskomatic.json`:

```json
{
    "GitHubProjects": ["ForNeVeR/s592", "ForNeVeR/taskomatic"],
    "TaskWarriorPath": "E:\\Programs\\msys2\\usr\\bin\\task.exe",
    "GitHubAccessToken": "something"
}
```

`GitHubAccessToken` is optional, and should be used if you want to overcome
GitHub rate limits or if you need to access private repositories. To get a
personal access token, visit [Personal access tokens][tokens] page.
Unfortunately, to access your private repositories, Taskomatic requires `repo`
scope, because for now GitHub doesn't provide more granular scopes for 
read-only issue-only access.

[taskwarrior]: https://taskwarrior.org/
[tokens]: https://github.com/settings/tokens
