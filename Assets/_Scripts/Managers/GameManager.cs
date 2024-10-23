using UnityEngine;
using Unity.Netcode;
using Fishing.Variables;
using Fishing.Actions;
using System.Linq;
using System.Collections;

namespace Fishing.Gameplay
{
    public class GameManager : NetworkBehaviour
    {
        #region Network Variables
        public NetworkVariable<bool> hasGameEnded = new NetworkVariable<bool>(false);
        public NetworkVariable<uint> timer = new NetworkVariable<uint>(0);
        #endregion

        [Header("Fields")]
        [SerializeField] private Fish normalFishPrefab;
        [SerializeField] private Fish rareFishPrefab;
        [SerializeField] private GameObject gameplayCanvasPrefab;
        [SerializeField] private int maxTime;
        [SerializeField] private int fishesInPool;

        [Header("Scriptable Objects")]
        [SerializeField] private PlayersListVariable players;
        [SerializeField] private IntVariable timerVariable;
        [SerializeField] private FishListVariable fishes;
        [SerializeField] private BulletsManager bulletsManager;
        [SerializeField] private HitFishVariable hitFishVariable;
        [SerializeField] private ActionSO gameEndAction;

        private bool _hasGameStarted;
        private Coroutine spawnFishCoroutine;

        #region Class Methods
        private void OnEnable()
        {
            players.onListValueChange += OnPlayersUpdated;
            hasGameEnded.OnValueChanged += OnGameStatusChange;
            hitFishVariable.onValueChange += HitFish;
            timer.OnValueChanged += OnTimerChanged;
        }

        private void OnDisable()
        {
            players.onListValueChange -= OnPlayersUpdated;
            hasGameEnded.OnValueChanged -= OnGameStatusChange;
            hitFishVariable.onValueChange -= HitFish;
            timer.OnValueChanged += OnTimerChanged;
        }

        private void Awake()
        {
            bulletsManager.Initialize();
        }

        private void Start()
        {
            fishes.value.Clear();
            StartCoroutine(PopulateFishPool());

            if (!IsServer)
                Instantiate(gameplayCanvasPrefab);
        }

        private IEnumerator PopulateFishPool()
        {
            if (!IsServer)
                yield break;

            for (int i = 0; i < fishesInPool; i++)
            {
                int num = Random.Range(0, 4);
                var fish = Instantiate(num == 2 ? rareFishPrefab : normalFishPrefab, transform.position, Quaternion.identity);
                var fishNetworkObject = fish.GetComponent<NetworkObject>();
                fishNetworkObject.Spawn();
                fish.fishID.Value = (uint)i;

                if (i % 2 == 0)
                {
                    yield return new WaitForEndOfFrame();
                }
            }
        }

        private IEnumerator SpawnFishCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1);

                var fish = fishes.value.FirstOrDefault(f => !f.IsActive);

                if (fish)
                {
                    fish.ActivateFish();
                }

                timer.Value += 1;

                if(maxTime - timer.Value <= 0)
                {
                    hasGameEnded.Value = true;
                    yield break;
                }
            }
        }

        private void HitFish(HitFishParams hitFish)
        {
            HitFishServerRpc(NetworkManager.LocalClientId, hitFish.fishId, hitFish.bulletId);
        }

        private void OnTimerChanged(uint oldVal, uint newVal)
        {
            int timerVal = maxTime - (int)newVal;
            timerVariable.Set(timerVal);
        }
        #endregion


        #region RPCs
        [ServerRpc(RequireOwnership = false)]
        private void HitFishServerRpc(ulong clientId, uint fishID, uint bulletId)
        {
            if (hasGameEnded.Value)
                return;

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId } 
                }
            };

            var fish = fishes.value.FirstOrDefault(f => f.fishID.Value == fishID && f.IsActive);

            if (fish)
            {
                fish.FishHit();
                var score = fish.Score;
                var player = players.value.FirstOrDefault(p => p.OwnerClientId == clientId);
                if (player)
                {
                    player.score.Value += (uint)score;
                    GotHitScoreClientRpc(bulletId, true, clientRpcParams);
                    return;
                }
            }

            GotHitScoreClientRpc(bulletId, false, clientRpcParams);
        }

        [ClientRpc]
        private void GotHitScoreClientRpc(uint bulletId, bool gotHitScore, ClientRpcParams rpcParams = default)
        {
            if (hasGameEnded.Value)
                return;

            var player = players.value.FirstOrDefault(p => p.IsLocalPlayer);
            player.AddBulletResult(bulletId, gotHitScore);
        }
        #endregion


        #region Listeners
        private void OnGameStatusChange(bool oldVal, bool newVal)
        {
            if (!newVal)
                return;

            StopAllCoroutines();
            gameEndAction.Execute();
        }

        private void OnPlayersUpdated(PlayerManager player)
        {
            if (!IsServer)
                return;

            if(_hasGameStarted && players.value.Count == 0)
            {
                fishes.value.ForEach(f =>
                {
                    if (f.IsActive)
                        f.ResetFish();
                });
                timer.Value = 0;
                StopCoroutine(spawnFishCoroutine);
                _hasGameStarted = false;
                return;
            }

            if (!_hasGameStarted && players.value.Count > 0)
            {
                spawnFishCoroutine = StartCoroutine(SpawnFishCoroutine());
                timer.Value = 0;
                players.value.ForEach(p => p.score.Value = 0);
                _hasGameStarted = true;
            }
        }
        #endregion


        #region NetworkBehaviour Callbacks
        public override void OnNetworkSpawn()
        {
            _hasGameStarted = false;
        }
        #endregion
    }
}
