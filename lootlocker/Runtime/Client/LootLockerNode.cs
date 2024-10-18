using Godot;
using LootLocker.Requests;

namespace LootLocker
{
    [Tool]
    public partial class LootLockerNode : Node
    {
        LootLockerAPIManager.RemoteSessionPoller poller;
        LootLockerServerApi serverApi;
        Yielder yielder = new();
        private bool initialized = false;

        private string _lootlocker_api_key = LootLockerConfig.current.apiKey;
        private string _lootlocker_domain_key = LootLockerConfig.current.domainKey;
        private string _lootlocker_game_version = LootLockerConfig.current.game_version;

        [Export]
        public string lootlocker_api_key
        {
            get => _lootlocker_api_key;
            set
            {
                _lootlocker_api_key = value;
                RefreshSDK();
            }
        }

        [Export]
        public string lootlocker_domain_key
        {
            get => _lootlocker_domain_key;
            set
            {
                _lootlocker_domain_key = value;
                RefreshSDK();
            }
        }

        [Export]
        public string lootlocker_game_version
        {
            get => _lootlocker_game_version;
            set
            {
                _lootlocker_game_version = value;
                RefreshSDK();
            }
        }

        public override void _Ready()
        {
            serverApi = LootLockerServerApi.Instantiate();
            serverApi.yielder = yielder;
            poller = new() { yielder = yielder };

            // Force initialize Godot task scheduler.
            new GodotTaskScheduler();
            initialized = true;
        }

        private void RefreshSDK()
        {
            LootLockerSDKManager.Init(_lootlocker_api_key, _lootlocker_game_version, _lootlocker_domain_key);
        }

        public override void _Process(double delta)
        {
            if (!initialized) return;
            yielder.ProcessCoroutines();
        }
    }
}
