using Ionic.Zip;
using TeuJson;

namespace FortLauncher.Mods;

public static class ModManager 
{
    public static string[] GetAllMods(Client client) 
    {
        if (client == null)
            return Array.Empty<string>();
        var directoryMod = Directory.GetDirectories(Path.Combine(client.Path, "Mods"))
            .Where(x => File.Exists(Path.Combine(x, "meta.json")))
            .Select(x => Path.GetFileName(x));

        var zippedMod = Directory.GetFiles(Path.Combine(client.Path, "Mods"))
            .Where(CheckIfValidZippedMod)
            .Select(x => Path.GetFileName(x));
        return zippedMod.Union(directoryMod).ToArray();
    }

    public static bool CheckIfValidZippedMod(string zip)
    {
        if (!zip.EndsWith(".zip"))
            return false;

        using var zipFile = ZipFile.Read(zip);
        if (!zipFile.ContainsEntry("meta.json"))
            return false;
        return true;
    }

    public static HashSet<string> GetBlacklist(Client client) 
    {
        var hashSet = new HashSet<string>();
        if (client is null)
            return hashSet;

        var blacklistPath = Path.Combine(client.Path, "Mods", "blacklist.txt");

        if (!File.Exists(blacklistPath))
            return hashSet;
        
        var blacklistFile = JsonTextReader.FromFile(blacklistPath).AsJsonArray;

        foreach (JsonValue arr in blacklistFile) 
        {
            if (!arr.IsString)
                continue;
            hashSet.Add(arr.AsString.Trim());
        }
        
        return hashSet;
    }

    public static void ToggleBlacklist(Client client, string mod, HashSet<string> set) 
    {
        if (!set.Contains(mod)) 
        {
            AddToBlacklist(client, mod, set);
            return;
        }
        RemoveToBlacklist(client, mod, set);
    }

    public static void AddToBlacklist(Client client, string mod, HashSet<string> set) 
    {
        set.Add(mod);
        UpdateBlacklist(client, set);
    }

    public static void RemoveToBlacklist(Client client, string mod, HashSet<string> set) 
    {
        set.Remove(mod);
        UpdateBlacklist(client, set);
    }

    public static void UpdateBlacklist(Client client, HashSet<string> set) 
    {
        var blacklistPath = Path.Combine(client.Path, "Mods", "blacklist.txt");       
        if (!File.Exists(blacklistPath))
            return;
        
        var jsonArray = new JsonArray();
        foreach (var s in set) 
        {
            jsonArray.Add(s);
        }
        
        JsonTextWriter.WriteToFile(blacklistPath, jsonArray);
    }

    public static bool IsBlacklisted(string mod, HashSet<string> set) 
    {
        return set.Contains(mod);
    }
}