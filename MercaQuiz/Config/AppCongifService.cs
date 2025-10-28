using System.Text.Json;

namespace MercaQuiz.Config;

public sealed class AppConfig
{
    public string BasePath { get; set; }
    public string DbFileName { get; set; }

    public AppConfig()
    {
        BasePath = FileSystem.AppDataDirectory ?? ".";
        DbFileName = "MercaQuiz.db3";
    }

    public string GetDatabaseFullPath() => Path.Combine(BasePath, DbFileName);
}

public static class AppConfigService
{
    private const string ConfigFileName = "mercaquiz.config.json";
    private static readonly object _sync = new();
    private static AppConfig? _current;
    private static string? _configPathCache;

    public static string ConfigPath => _configPathCache ??= ComputeConfigPath();

    public static AppConfig Current
    {
        get
        {
            if (_current != null) return _current;
            lock (_sync)
            {
                if (_current != null) return _current;
                _current = LoadOrCreate();
                return _current;
            }
        }
    }

    private static AppConfig LoadOrCreate()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                if (cfg != null)
                    return cfg;
            }
        }
        catch
        {
            // ignora errori di lettura/parse
        }

        var def = new AppConfig();
        Save(def);
        return def;
    }

    public static void Save(AppConfig cfg)
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
            _current = cfg;
        }
        catch
        {
            // non fatale; se la scrittura fallisce, ignoriamo (log eventualmente)
        }
    }

    public static void SaveCurrent() => Save(Current);

    public static string GetDatabaseFullPath() => Current.GetDatabaseFullPath();

    // Try to place config next to the app (AppContext.BaseDirectory). If not writable, fallback to FileSystem.AppDataDirectory.
    private static string ComputeConfigPath()
    {
        // prefer executable folder for portability
        var exeDir = AppContext.BaseDirectory;
        if (!string.IsNullOrWhiteSpace(exeDir))
        {
            var candidate = Path.Combine(exeDir, ConfigFileName);
            if (IsDirectoryWritable(exeDir))
                return candidate;
        }

        // fallback: app data directory (always writable)
        var appDataCandidate = Path.Combine(FileSystem.AppDataDirectory ?? ".", ConfigFileName);
        return appDataCandidate;
    }

    // Quick writable check for a directory (attempt to create and remove a temp file)
    private static bool IsDirectoryWritable(string dir)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dir)) return false;
            if (!Directory.Exists(dir)) return false;
            var testFile = Path.Combine(dir, $".writecheck_{Guid.NewGuid():N}.tmp");
            using (var fs = File.Create(testFile))
            {
                // write a single byte
                fs.WriteByte(0);
                fs.Flush();
            }
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }
}