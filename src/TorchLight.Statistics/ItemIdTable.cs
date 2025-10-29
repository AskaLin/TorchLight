using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace TorchLight.Statistics
{
    public partial class ItemIdTable
    {        
        public static Dictionary<int, string> GetIdTable()
        {
            var fileName = "ItemIdTable.json";

            // Candidate locations to look for the config file (runtime and development layouts)
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, fileName),
                Path.Combine(Directory.GetCurrentDirectory(), fileName),
                Path.Combine(AppContext.BaseDirectory, "src", "TorchLight.Statistics", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "TorchLight.Statistics", fileName)
            };

            string foundPath = null;
            foreach (var p in candidates)
            {
                if (File.Exists(p))
                {
                    foundPath = p;
                    break;
                }
            }

            if (foundPath == null)
            {
                throw new FileNotFoundException($"Could not find '{fileName}' in expected locations.");
            }

            var json = File.ReadAllText(foundPath, Encoding.UTF8);

            var result = new Dictionary<int, string>();

            try
            {
                var doc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (doc != null)
                {
                    foreach (var kv in doc)
                    {
                        if (!int.TryParse(kv.Key.Trim(), out var id))
                            continue;

                        string name = null;
                        var el = kv.Value;

                        if (el.ValueKind == JsonValueKind.String)
                        {
                            name = el.GetString();
                        }
                        else if (el.ValueKind == JsonValueKind.Object)
                        {
                            if (el.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
                                name = nameProp.GetString();
                            else if (el.TryGetProperty("Name", out var nameProp2) && nameProp2.ValueKind == JsonValueKind.String)
                                name = nameProp2.GetString();
                        }

                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        if (!result.ContainsKey(id))
                            result[id] = name;
                    }
                }
            }
            catch
            {
                // ignore and return what we have (possibly empty)
            }

            return result;
        }

        [GeneratedRegex(@"^\s*(\d+)\s+(.+)$")]
        private static partial Regex IdTableRegex();
    }
}
