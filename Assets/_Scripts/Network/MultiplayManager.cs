using Unity.Services.Core;
using UnityEngine;
#if !UNITY_WEBGL
using Unity.Services.Multiplay;
#endif
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;
using Fishing.Models;
using System.Collections;
using Unity.Services.Matchmaker.Models;
using System.Collections.Generic;
using Unity.Services.Matchmaker;

namespace Fishing.Network
{
    public class MultiplayManager : MonoBehaviourSingleton<MultiplayManager>
    {
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private string myQueueName;
        private string _ticketId;

#if !UNITY_WEBGL
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

                await CreateBackfillTicket();
                StartCoroutine(ApproveBackfillTicketEverySecond());
            }
        }

        private async Task CreateBackfillTicket()
        {
            MatchmakingResults results =
                await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();

            Debug.Log(
                $"Environment: {results.EnvironmentId} MatchId: {results.MatchId} MatchProperties: {results.MatchProperties}");

            var backfillTicketProperties = new BackfillTicketProperties(results.MatchProperties);

            string queueName = myQueueName; // must match the name of the queue you want to use in matchmaker
            string connectionString = MultiplayService.Instance.ServerConfig.IpAddress + ":" +
                                      MultiplayService.Instance.ServerConfig.Port;

            var options = new CreateBackfillTicketOptions(queueName,
                connectionString,
                new Dictionary<string, object>(),
                backfillTicketProperties);

            // Create backfill ticket
            Debug.Log("Requesting backfill ticket");
            _ticketId = await MatchmakerService.Instance.CreateBackfillTicketAsync(options);
        }

        private IEnumerator ApproveBackfillTicketEverySecond()
        {
            for (int i = 2; i >= 0; i--)
            {
                Debug.Log($"Waiting {i} seconds to start backfill");
                yield return new WaitForSeconds(1f);
            }

            while (true)
            {
                yield return new WaitForSeconds(1f);
                if (string.IsNullOrWhiteSpace(_ticketId))
                {
                    Debug.Log("No backfill ticket to approve");
                    continue;
                }

                Debug.Log("Doing backfill approval for _ticketId: " + _ticketId);
                yield return MatchmakerService.Instance.ApproveBackfillTicketAsync(_ticketId);
                Debug.Log("Approved backfill ticket: " + _ticketId);
            }
        }
#endif

        public void JoinServer(string ip, ushort port)
        {
            UnityTransport transport = networkManager.GetComponent<UnityTransport>();
            transport.SetConnectionData(ip, port);
            networkManager.StartClient();
        }
    }
}
