using Godot;

namespace LootLocker
{
    public class LootLockerConfig
    {
        public virtual string SettingName { get { return "LootLockerConfig"; } }
        public virtual string ConfigPath { get { return "res://lootlocker.cfg"; } }

        private static void Init()
        {
            _current = new();
            ConfigFile config = new ConfigFile();
            if (Error.Ok == config.Load(current.ConfigPath))
            {
                GD.Print("Loaded");
                _current.apiKey = config.GetValue(_current.SettingName, "apiKey").AsString() ?? null;
                _current.game_version = config.GetValue(_current.SettingName, "game_version", "").AsString();
                _current.currentDebugLevel = (DebugLevel)config.GetValue(_current.SettingName, "debugLevel", (int)DebugLevel.All).AsInt32();
                _current.allowTokenRefresh = config.GetValue(_current.SettingName, "allowTokenRefresh").AsBool();
                _current.domainKey = config.GetValue(_current.SettingName, "domainKey").AsString() ?? null;
                _current.token = config.GetValue(_current.SettingName, "token").AsString() ?? null;
                _current.refreshToken = config.GetValue(_current.SettingName, "refreshToken").AsString() ?? null;
                _current.gameID = config.GetValue(_current.SettingName, "gameID").AsInt32();
                _current.deviceID = config.GetValue(_current.SettingName, "deviceID").AsString() ?? null;
            }
            else
            {
                config.SetValue(_current.SettingName, "apiKey", _current.apiKey);
                config.SetValue(_current.SettingName, "game_version", _current.game_version);
                config.SetValue(_current.SettingName, "currentDebugLevel", (int)_current.currentDebugLevel);
                config.SetValue(_current.SettingName, "allowTokenRefresh", _current.allowTokenRefresh);
                config.SetValue(_current.SettingName, "domainKey", _current.domainKey);
                config.SetValue(_current.SettingName, "token", _current.token);
                config.SetValue(_current.SettingName, "refreshToken", _current.refreshToken);
                config.SetValue(_current.SettingName, "gameID", _current.gameID);
                config.SetValue(_current.SettingName, "deviceID", _current.deviceID);
            }
            GD.Print("Saving file");
            config.Save(current.ConfigPath);
        }

        public static bool CreateNewSettings(string apiKey, string gameVersion, string domainKey, LootLockerConfig.DebugLevel debugLevel = DebugLevel.All, bool allowTokenRefresh = false)
        {
            Init();

            _current.apiKey = apiKey;
            _current.game_version = gameVersion;
            _current.currentDebugLevel = debugLevel;
            _current.allowTokenRefresh = allowTokenRefresh;
            _current.domainKey = domainKey;
            _current.token = null;
            _current.refreshToken = null;
            _current.gameID = 0;
            _current.deviceID = null;
            _current.ConstructUrls();
            return true;
        }

        public static bool ClearSettings()
        {
            _current.apiKey = null;
            _current.game_version = null;
            _current.currentDebugLevel = DebugLevel.All;
            _current.allowTokenRefresh = true;
            _current.domainKey = null;
            _current.token = null;
            _current.refreshToken = null;
            _current.gameID = 0;
            _current.deviceID = null;
            return true;
        }

        private void ConstructUrls()
        {
            string startOfUrl = UrlProtocol;
            if (!string.IsNullOrEmpty(domainKey))
            {
                startOfUrl += domainKey + ".";
            }
            adminUrl = startOfUrl + GetUrlCore() + AdminUrlAppendage;
            playerUrl = startOfUrl + GetUrlCore() + PlayerUrlAppendage;
            userUrl = startOfUrl + GetUrlCore() + UserUrlAppendage;
            baseUrl = startOfUrl + GetUrlCore();
        }

        private static LootLockerConfig _current;

        public static LootLockerConfig current
        {
            get
            {
                if (_current == null)
                {
                    Init();
                    _current.ConstructUrls();
                }

                return _current;
            }
        }

        public (string key, string value) dateVersion = ("LL-Version", "2021-03-01");
        public string apiKey;

        public string adminToken;

        public string token;

        public string refreshToken;

        public string domainKey;

        public int gameID;
        public string game_version = "1.0.0.0";

        public string sdk_version = "";

        public string deviceID = "defaultPlayerId";

        private static readonly string UrlProtocol = "https://";
        private static readonly string UrlCore = "api.lootlocker.io";

        private static string UrlCoreOverride =
#if LOOTLOCKER_TARGET_STAGE_ENV
           "api.stage.internal.dev.lootlocker.cloud";
#else
            null;
#endif
        private static string GetUrlCore() { return string.IsNullOrEmpty(UrlCoreOverride) ? UrlCore : UrlCoreOverride; }

        public static bool IsTargetingProductionEnvironment()
        {
            return string.IsNullOrEmpty(UrlCoreOverride) || UrlCoreOverride.Equals(UrlCore);

        }
        private static readonly string UrlAppendage = "/v1";
        private static readonly string AdminUrlAppendage = "/admin";
        private static readonly string PlayerUrlAppendage = "/player";
        private static readonly string UserUrlAppendage = "/game";

        public string url = UrlProtocol + GetUrlCore() + UrlAppendage;

        public string adminUrl = UrlProtocol + GetUrlCore() + AdminUrlAppendage;
        public string playerUrl = UrlProtocol + GetUrlCore() + PlayerUrlAppendage;
        public string userUrl = UrlProtocol + GetUrlCore() + UserUrlAppendage;
        public string baseUrl = UrlProtocol + GetUrlCore();
        public float clientSideRequestTimeOut = 180f;
        public enum DebugLevel { All, ErrorOnly, NormalOnly, Off, AllAsNormal }
        public DebugLevel currentDebugLevel = DebugLevel.All;
        public bool allowTokenRefresh = true;
    }
}
