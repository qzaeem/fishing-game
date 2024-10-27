using Unity.Services.Core;
using UnityEngine;
using Unity.Services.Multiplay;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;

namespace Fishing.Network
{
    public class MultiplayManager : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;

        private IServerQueryHandler serverQueryHandler;

        private async void Start()
        {
            if (Application.platform == RuntimePlatform.LinuxServer)
            {
                Application.targetFrameRate = 30;
                await UnityServices.InitializeAsync();

                ServerConfig serverConfig = MultiplayService.Instance.ServerConfig;
                serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(4, "FishingServer", "Fishing", "0", "TestMap");

                if (serverConfig.AllocationId != string.Empty)
                {
                    networkManager.GetComponent<UnityTransport>().SetConnectionData("0.0.0.0", serverConfig.Port, "0.0.0.0");
                    networkManager.StartServer();
                    await MultiplayService.Instance.ReadyServerForPlayersAsync();
                }
            }
        }

        private async void Update()
        {
            if (Application.platform == RuntimePlatform.LinuxServer)
            {
                if(serverQueryHandler != null)
                {
                    serverQueryHandler.CurrentPlayers = (ushort)networkManager.ConnectedClientsIds.Count;
                    serverQueryHandler.UpdateServerCheck();
                    await Task.Delay(100);
                }
            }
        }

        public void JoinServer()
        {
            UnityTransport transport = networkManager.GetComponent<UnityTransport>();
            //transport.SetConnectionData();
            networkManager.StartClient();
        }
    }
}
