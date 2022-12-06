using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Taskomatic.Core;

// Accessors are used during deserialization:
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
// Class is instantiated during deserialization:
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class Config
{
    public const string ConfigFileName = ".taskomatic.json";

    public string[] GitHubProjects { get; set; } = null!;
    public string? TaskWarriorPath { get; set; }
    public string[]? TaskWarriorCommand { get; set; }
    public string? GitHubAccessToken { get; set; }

    public static Task<Config> Read(Stream input)
    {
        return Task.Run(() =>
        {
            var serializer = new JsonSerializer();
            using var streamReader = new StreamReader(input);
            using var reader = new JsonTextReader(streamReader);
            return serializer.Deserialize<Config>(reader);
        });
    }
}
