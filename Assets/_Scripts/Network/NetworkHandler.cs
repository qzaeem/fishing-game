using UnityEngine;
using Unity.Netcode;
using Fishing.Models;
using UnityEngine.SceneManagement;

namespace Fishing.Network
{
    public class NetworkHandler : MonoBehaviourSingleton<NetworkHandler>
    {
        [SerializeField] private string gameSceneName;
        [SerializeField] private string menuSceneName;
        private NetworkManager _networkManager;

        public  void OnEnable()
        {
            _networkManager = NetworkManager.Singleton;

            _networkManager.OnServerStarted += OnServerStarted;
            _networkManager.OnServerStopped += OnServerStopped;
            _networkManager.ConnectionApprovalCallback += ApprovalCheck;
            _networkManager.OnClientDisconnectCallback += OnDisconnected;
        }

        private void OnDestroy()
        {
            _networkManager.OnServerStarted -= OnServerStarted;
            _networkManager.OnServerStopped -= OnServerStopped;
            _networkManager.ConnectionApprovalCallback -= ApprovalCheck;
            _networkManager.OnClientDisconnectCallback -= OnDisconnected;
        }

        public void StartHost()
        {
            _networkManager.StartHost();
        }

        public void StartServer()
        {
            _networkManager.StartServer();
        }

        public void StartClient()
        {
            _networkManager.StartClient();
        }

        #region Callbacks
        private void OnServerStarted()
        {
            _networkManager.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        private void OnServerStopped(bool stopped)
        {

        }

        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            int currentPlayers = _networkManager.ConnectedClients.Count;

            if (currentPlayers < 2)
            {
                response.Approved = true;
                response.CreatePlayerObject = true;
            }
            else
            {
                response.Approved = false;
                response.CreatePlayerObject = false;
                response.Position = null;
                response.Rotation = null;
            }
        }

        private void OnDisconnected(ulong clientId)
        {
            if(clientId == _networkManager.LocalClientId)
                SceneManager.LoadScene(0);
        }
        #endregion

    }
}
