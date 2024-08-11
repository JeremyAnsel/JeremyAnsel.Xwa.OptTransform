using JeremyAnsel.Xwa.HooksConfig;

namespace JeremyAnsel.Xwa.OptTransform
{
    public static class OptTransformHelpers
    {
        public static string GetBaseOptFilename(string? filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return string.Empty;
            }

            string baseFilename = Path.ChangeExtension(filename, null);

            if (baseFilename.EndsWith("exterior", StringComparison.OrdinalIgnoreCase))
            {
                baseFilename = baseFilename[..^"exterior".Length];
            }
            else if (baseFilename.EndsWith("cockpit", StringComparison.OrdinalIgnoreCase))
            {
                baseFilename = baseFilename[..^"cockpit".Length];
            }

            return baseFilename;
        }

        public static Dictionary<string, List<int>> GetObjectProfiles(string? filename)
        {
            var profiles = new Dictionary<string, List<int>>
            {
                ["Default"] = new List<int>()
            };

            if (string.IsNullOrEmpty(filename))
            {
                return profiles;
            }

            string shipPath = GetBaseOptFilename(filename);

            var lines = XwaHooksConfig.GetFileLines(shipPath + "ObjectProfiles.txt");

            if (lines.Count == 0)
            {
                lines = XwaHooksConfig.GetFileLines(shipPath + ".ini", "ObjectProfiles");
            }

            foreach (string line in lines)
            {
                int pos = line.IndexOf('=');

                if (pos == -1)
                {
                    continue;
                }

                string name = line[..pos].Trim();

                if (name.Length == 0)
                {
                    continue;
                }

                var values = XwaHooksConfig.Tokennize(line[(pos + 1)..].Trim());
                var indices = new List<int>();

                foreach (string value in values)
                {
                    int index = XwaHooksConfig.ToInt32(value);
                    indices.Add(index);
                }

                profiles[name] = indices;
            }

            return profiles;
        }

        public static List<string> GetSkins(string? filename)
        {
            var skins = new List<string>();

            if (string.IsNullOrEmpty(filename))
            {
                return skins;
            }

            string optName = Path.GetFileNameWithoutExtension(filename);
            string directory = Path.Combine(Path.GetDirectoryName(filename)!, "Skins", optName);

            if (Directory.Exists(directory))
            {
                foreach (string path in Directory.EnumerateDirectories(directory))
                {
                    skins.Add(Path.GetFileName(path));
                }

                foreach (string path in Directory.EnumerateFiles(directory, "*.zip"))
                {
                    skins.Add(Path.GetFileNameWithoutExtension(path));
                }

                foreach (string path in Directory.EnumerateFiles(directory, "*.7z"))
                {
                    skins.Add(Path.GetFileNameWithoutExtension(path));
                }
            }

            return skins;
        }
    }
}
