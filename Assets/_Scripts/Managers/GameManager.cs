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
        [SerializeField] private Fish superRareFishPrefab;
        [SerializeField] private GameObject gameplayCanvasPrefab;

        [Header("Scriptable Objects")]
        [SerializeField] private PlayersListVariable players;
        [SerializeField] private IntVariable timerVariable;
        [SerializeField] private FishListVariable fishes;
        [SerializeField] private BulletsManager bulletsManager;
        [SerializeField] private HitFishVariable hitFishVariable;
        [SerializeField] private ActionSO gameEndAction;
        [SerializeField] private ConfigVariable gameConfig;

        private bool _hasGameStarted;
        private int maxTime;
        private int fishesInPool;
        private Coroutine spawnFishCoroutine;
        private Coroutine timerCoroutine;

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
            fishesInPool = gameConfig.value.fishesInPool;
            maxTime = gameConfig.value.maxGameTime;
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
                int num = Random.Range(0, 10);
                Fish prefab = normalFishPrefab;
                if (num == 1 || num == 6)
                    prefab = superRareFishPrefab;
                else if (num == 0 || num == 5 || num == 8)
                    prefab = rareFishPrefab;

                var fish = Instantiate(prefab, transform.position, Quaternion.identity);
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
                yield return new WaitForSeconds(0.5f);

                int rand = Random.Range(1, 9);

                for(int i = 0; i < rand; i++)
                {
                    var fish = fishes.value.FirstOrDefault(f => !f.IsActive);

                    if (fish)
                    {
                        fish.ActivateFish();
                    }
                }
            }
        }

        private IEnumerator TimerCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1);

                timer.Value += 1;

                if (maxTime - timer.Value <= 0)
                {
                    hasGameEnded.Value = true;
                    yield break;
                }
            }
        }

        private void HitFish(HitFishParams hitFish)
        {
            HitFishServerRpc(NetworkManager.LocalClientId, hitFish.fishId, hitFish.bulletId, hitFish.damage);
        }

        private void OnTimerChanged(uint oldVal, uint newVal)
        {
            int timerVal = maxTime - (int)newVal;
            timerVariable.Set(timerVal);
        }
        #endregion


        #region RPCs
        [ServerRpc(RequireOwnership = false)]
        private void HitFishServerRpc(ulong clientId, uint fishID, uint bulletId, uint damage)
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
                var score = fish.FishHit((int)damage);
                var player = players.value.FirstOrDefault(p => p.OwnerClientId == clientId);
                if (player)
                {
                    if(score > 0) player.score.Value += (uint)score;
                    GotHitScoreClientRpc(bulletId, (uint)score, clientRpcParams);
                    return;
                }
            }

            GotHitScoreClientRpc(bulletId, 0, clientRpcParams);
        }

        [ClientRpc]
        private void GotHitScoreClientRpc(uint bulletId, uint gotHitScore, ClientRpcParams rpcParams = default)
        {
            if (hasGameEnded.Value)
                return;

            var player = players.value.FirstOrDefault(p => p.IsLocalPlayer);
            player.AddBulletResult(bulletId, (int)gotHitScore);
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
                StopCoroutine(timerCoroutine);
                _hasGameStarted = false;
                return;
            }

            if (!_hasGameStarted && players.value.Count > 0)
            {
                spawnFishCoroutine = StartCoroutine(SpawnFishCoroutine());
                timerCoroutine = StartCoroutine(TimerCoroutine());
                timer.Value = 0;
                players.value.ForEach(p => p.score.Value = 0);
                _hasGameStarted = true;
            }

            if (players.value.Contains(player))
            {
                int freeSlot = -1;

                for(int i = 0; i < 5; i++)
                {
                    if (players.value.Any(p => (int)p.slot.Value == i))
                        continue;

                    freeSlot = i;
                    break;
                }

                if(freeSlot > -1)
                {
                    player.slot.Value = (uint)freeSlot;
                }
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
