
using Godot;
using LootLockerTestConfigurationUtils;

namespace LootLocker
{
    public partial class LootLockerNode : Node
    {
        LootLockerAPIManager.RemoteSessionPoller poller;
        LootLockerCIRetry CIRetry;
        LootLockerServerApi serverApi;
        Yielder yielder = new();

        public LootLockerNode()
        {
            serverApi = LootLockerServerApi.Instantiate();
            serverApi.yielder = yielder ;
            poller = new() { yielder = yielder };
            CIRetry = new() { yielder = yielder };
        }

        public override void _Process(double delta)
        {
            yielder.ProcessCoroutines();
        }

        public override void _PhysicsProcess(double delta)
        {
            yielder.ProcessCoroutines();
        }

    }
}
