using System.Text;
using System.Text.RegularExpressions;

namespace TorchLight.Statistics
{
    public partial class IdTable
    {        
        public static Dictionary<int, string> GetIdTable()
        {
            var dict = new Dictionary<int, string>();
            var fileName = "IdTable.conf";

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

            var lines = File.ReadAllLines(foundPath, Encoding.UTF8);
            
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var line = raw.Trim();

                // skip code fences or metadata that may appear in some contexts
                if (line.StartsWith("```") || line.StartsWith("src ") || line.StartsWith("src\\"))
                    continue;

                var m = IdTableRegex().Match(line);
                if (!m.Success)
                    continue;

                var id = Convert.ToInt32(m.Groups[1].Value.Trim());
                var name = m.Groups[2].Value.Trim();

                if (!dict.ContainsKey(id))
                    dict[id] = name;
            }

            return dict;
        }

        [GeneratedRegex(@"^\s*(\d+)\s+(.+)$")]
        private static partial Regex IdTableRegex();
    }
}
