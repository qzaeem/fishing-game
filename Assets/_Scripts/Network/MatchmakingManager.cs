using UnityEngine;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Collections;

namespace Fishing.Network
{
    public class MatchmakingManager : MonoBehaviour
    {
        [SerializeField] private string queueName;
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private MainMenuCanvas mainMenuCanvas;

        private string _currentTicket;
        private bool isDeallocating = false;
        private bool deallocatingCancellationToken = false;

        private IEnumerator Start()
        {
            if(Application.platform != RuntimePlatform.LinuxServer)
            {
                mainMenuCanvas.ShowLoading(true);
                var initTask = UnityServices.InitializeAsync();
                while (!initTask.IsCompleted) yield return null;
                AuthenticationService.Instance.SwitchProfile(Random.Range(0, 1000000).ToString());
                var authTask = AuthenticationService.Instance.SignInAnonymouslyAsync();
                while (!authTask.IsCompleted) yield return null;
                mainMenuCanvas.ShowLoading(false);
            }
        }

        //private void Update()
        //{
        //    if (Application.platform == RuntimePlatform.LinuxServer)
        //    {
        //        if (networkManager.ConnectedClientsList.Count == 0 && !isDeallocating)
        //        {
        //            isDeallocating = true;
        //            deallocatingCancellationToken = false;
        //            DeAllocate();
        //        }

        //        if (networkManager.ConnectedClientsList.Count > 0)
        //        {
        //            isDeallocating = false;
        //            deallocatingCancellationToken = true;
        //        }
        //    }
        //}

        //private async void DeAllocate()
        //{
        //    await Task.Delay(1000 * 60);

        //    if (!deallocatingCancellationToken)
        //    {
        //        Application.Quit();
        //    }
        //}

        public void JoinSessionAsClient()
        {
            StartCoroutine(ClientJoin());
        }

        private IEnumerator ClientJoin()
        {
            CreateTicketOptions createTicketOptions = new CreateTicketOptions(queueName,
                new Dictionary<string, object>());
            List<Player> players = new List<Player> { new Player(AuthenticationService.Instance.PlayerId,
                new Dictionary<string, object>()) };

            // Create a matchmaking ticket
            var createTicketTask = MatchmakerService.Instance.CreateTicketAsync(players, createTicketOptions);
            while (!createTicketTask.IsCompleted)
            {
                yield return null;
            }

            if (createTicketTask.IsCompletedSuccessfully)
            {
                CreateTicketResponse createTicketResponse = createTicketTask.Result;
                _currentTicket = createTicketResponse.Id;

                // Poll for the ticket status
                while (Application.isPlaying)
                {
                    var getTicketTask = MatchmakerService.Instance.GetTicketAsync(_currentTicket);
                    while (!getTicketTask.IsCompleted) yield return null;
                    if (getTicketTask.IsCompletedSuccessfully)
                    {
                        TicketStatusResponse ticketStatusResponse = getTicketTask.Result;

                        // Check if the ticket has an assignment and is of the type MultiplayAssignment
                        if (ticketStatusResponse?.Value is MultiplayAssignment multiplayAssignment)
                        {
                            switch (multiplayAssignment.Status)
                            {
                                case MultiplayAssignment.StatusOptions.Found:
                                    // Set connection data and start client
                                    UnityTransport transport = networkManager.GetComponent<UnityTransport>();
                                    transport.SetConnectionData(multiplayAssignment.Ip, ushort.Parse(multiplayAssignment.Port.ToString()));
                                    networkManager.StartClient();

                                    Debug.Log("Match Found!");
                                    yield break;

                                case MultiplayAssignment.StatusOptions.Timeout:
                                    Debug.Log("Match Timeout!");
                                    yield break;

                                case MultiplayAssignment.StatusOptions.Failed:
                                    Debug.Log("Match Failed: " + multiplayAssignment.Message);
                                    yield break;

                                case MultiplayAssignment.StatusOptions.InProgress:
                                    Debug.Log("Matching in progress!");
                                    break;
                            }
                        }
                        else
                        {
                            Debug.Log("Waiting for match assignment...");
                        }
                    }
                    else
                    {
                        Debug.LogError("Get ticket failed!");
                    }

                    yield return new WaitForSeconds(1);
                }

                Debug.Log("While loop ended!");
            }
            else
            {
                Debug.LogError("Create ticket filed!");
            }
        }
    }
}
