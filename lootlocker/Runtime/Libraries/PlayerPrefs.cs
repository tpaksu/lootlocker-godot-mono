using Godot;

public static class PlayerPrefs
{
    public static ConfigFile configFile = new();
    public const string SectionKey = "Config";

    private static void EnsureConfigLoaded()
    {
#pragma warning disable IDE0150 // Prefer 'null' check over type check
        if (configFile is null or not ConfigFile)
        {
            if (Error.Ok == configFile.LoadEncryptedPass("user://config.cfg", ""))
            {
            }
            else
            {
                configFile = new ConfigFile();
            }
        }
#pragma warning restore IDE0150 // Prefer 'null' check over type check
    }

    //Removes all keys and values from the preferences. Use with caution.
    public static void DeleteAll()
    {
        EnsureConfigLoaded();
        configFile.Clear();
    }
    //	Removes the given key from the PlayerPrefs. If the key does not exist, DeleteKey has no impact.
    public static void DeleteKey(string key)
    {
        EnsureConfigLoaded();
        configFile.SetValue(SectionKey, key, new Variant());
    }
    //	Returns the value corresponding to key in the preference file if it exists.
    public static float GetFloat(string key, Variant @default = default)
    {
        EnsureConfigLoaded();
        return (float)configFile.GetValue(SectionKey, key, @default).AsDouble();
    }
    //	Returns the value corresponding to key in the preference file if it exists.
    public static int GetInt(string key, Variant @default = default)
    {
        EnsureConfigLoaded();
        return configFile.GetValue(SectionKey, key, @default).AsInt32();
    }
    //	Returns the value corresponding to key in the preference file if it exists.
    public static string GetString(string key, Variant @default = default)
    {
        EnsureConfigLoaded();
        return configFile.GetValue(SectionKey, key, @default).AsString();
    }
    //	Returns true if the given key exists in PlayerPrefs, otherwise returns false.
    public static bool HasKey(string key)
    {
        EnsureConfigLoaded();
        return configFile.HasSectionKey(SectionKey, key);
    }
    //	Saves all modified preferences.
    public static void Save()
    {
        EnsureConfigLoaded();
        configFile.SaveEncryptedPass("user://config.cfg", "");
    }
    //	Sets the float value of the preference identified by the given key. You can use PlayerPrefs.GetFloat to retrieve this value.
    public static void SetFloat(string key, float value)
    {
        EnsureConfigLoaded();
        configFile.SetValue(SectionKey, key, value);
    }
    //	Sets a single integer value for the preference identified by the given key. You can use PlayerPrefs.GetInt to retrieve this value.
    public static void SetInt(string key, int value)
    {
        EnsureConfigLoaded();
        configFile.SetValue(SectionKey, key, value);
    }
    //	Sets a single string value for the preference identified by the given key. You can use PlayerPrefs.GetString to retrieve this value.
    public static void SetString(string key, string value)
    {
        EnsureConfigLoaded();
        configFile.SetValue(SectionKey, key, value);
    }
}
