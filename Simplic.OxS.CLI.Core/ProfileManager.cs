using Spectre.Console;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Simplic.OxS.CLI.Core
{
    public class ProfileManager(string path)
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public string? GetDefaultProfile()
        {
            try
            {
                var defaultPath = Path.Join(path, "default.txt");
                if (!File.Exists(defaultPath))
                    return null;

                var profileName = File.ReadAllText(defaultPath);
                var profilePath = GetProfilePath(profileName);
                if (!File.Exists(profilePath))
                    return null;

                return profileName;
            }
            catch
            {
                return null;
            }
        }

        public bool Select(string name)
        {
            var profile = GetProfilePath(name);
            if (!File.Exists(profile))
                return false;

            var defaultPath = Path.Join(path, "default.txt");
            File.WriteAllText(defaultPath, name);
            return true;
        }

        public bool Unselect()
        {
            var defaultPath = Path.Join(path, "default.txt");
            if (!File.Exists(defaultPath))
                return false;

            File.Delete(defaultPath);
            return true;
        }

        public bool Delete(string name)
        {
            var profile = GetProfilePath(name);
            if (!File.Exists(profile))
                return false;

            File.Delete(profile);
            return true;
        }

        public Profile? Load(string name)
        {
            try
            {
                using var file = File.OpenRead(GetProfilePath(name));
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonObject>>(file) ?? [];
                return new Profile { Data = data };
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public void Save(string name, Profile profile)
        {
            using var file = File.Create(GetProfilePath(name));
            JsonSerializer.Serialize(file, profile.Data, JsonOptions);
        }

        private string GetProfilePath(string name)
        {
            // https://stackoverflow.com/a/847251
            var invalid = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            var regex = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalid);
            var newName = Regex.Replace(name, regex, "_") + ".json";

            Directory.CreateDirectory(path);

            return Path.Join(path, newName);
        }
    }
}
