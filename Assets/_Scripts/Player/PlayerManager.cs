using UnityEngine;
using Unity.Netcode;
using Fishing.Variables;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Fishing.Actions;
using Unity.Collections;

namespace Fishing.Gameplay
{
    public class PlayerManager : NetworkBehaviour
    {
        #region Network Variables
        public NetworkVariable<uint> score = new NetworkVariable<uint>(0);
        public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>();
        #endregion

        [Header("Fields")]
        public Transform bulletPoolContainer;
        [SerializeField] private Transform bulletSpawnPoint;
        [SerializeField] private int bulletsInPool;

        [Header("Prefabs")]
        [SerializeField] private PlayerBullet bulletPrefab;

        [Header("Scriptable Object Variables")]
        [SerializeField] private PlayersListVariable players;
        [SerializeField] private BulletsManager bulletsManager;
        [SerializeField] private StringVariable playerNameVariable;
        [SerializeField] private ActionSO gameEndAction;
        [SerializeField] private ActionSO updatePlayerScoreAction;

        private List<PlayerBullet> _bullets = new List<PlayerBullet>();
        private Coroutine shootCoroutine;
        private bool _canShoot;

        #region Class Methods

        private void OnEnable()
        {
            gameEndAction.executeAction += OnGameEnd;
            score.OnValueChanged += OnPlayerScoreChanged;
        }

        private void OnDisable()
        {
            gameEndAction.executeAction -= OnGameEnd;
            score.OnValueChanged += OnPlayerScoreChanged;
        }

        private void Start()
        {
            players.Add(this);
            _canShoot = true;
            StartCoroutine(PopulateBulletPool());
            Vector3 pos = transform.position;
            pos.x = players.value.Count == 1 ? -2.5f : 2.5f;
            transform.position = pos;

            if (!this.LocalClientIsOwner())
                return;

            SetNameServerRpc(playerNameVariable.value);
        }

        private void Update()
        {
            if (!this.LocalClientIsOwner() || !_canShoot)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                shootCoroutine = StartCoroutine(ShootCoroutine());
            }
            else if (Input.GetMouseButtonUp(0))
            {
                StopCoroutine(shootCoroutine);
            }

            Vector3 mousePositionInWorld = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.position.z));
            Vector3 targetVector = mousePositionInWorld - transform.position;
            if (Vector2.Angle(transform.up, targetVector) > 0.5f)
            {
                Vector3 currentUp = transform.up;
                Vector3 newUp = Vector3.RotateTowards(currentUp, targetVector, Time.deltaTime * 5, 0);
                if (newUp.y < 0.4f) newUp.y = 0.4f;
                transform.rotation = Quaternion.LookRotation(transform.forward, newUp);
            }
        }

        private IEnumerator ShootCoroutine()
        {
            while (true)
            {
                var bullet = _bullets.FirstOrDefault(b => !b.IsActive);

                if (bullet)
                {
                    bullet.Shoot(bulletSpawnPoint.position, bulletSpawnPoint.up);
                    bulletsManager.AddActiveBullet(bullet);
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator PopulateBulletPool()
        {
            if (!IsServer)
                yield break;

            for (int i = 0; i < bulletsInPool; i++)
            {
                var bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
                bullet.BulletId = (uint)i;
                var bulletNetworkObject = bullet.GetComponent<NetworkObject>();
                bulletNetworkObject.SpawnWithOwnership(OwnerClientId);

                // Spawn bullets in multiple frames rather than one
                if (i % 2 == 0)
                {
                    yield return new WaitForEndOfFrame(); 
                }
            }
        }

        public void AddBullet(PlayerBullet bullet)
        {
            _bullets.Add(bullet);
        }

        public void AddBulletResult(uint bulletID, bool hasScoredAHit)
        {
            bulletsManager.AddBulletResult(bulletID, hasScoredAHit);
        }
        #endregion

        #region RPCs
        [ServerRpc]
        private void SetNameServerRpc(string name)
        {
            playerName.Value = name;
        }
        #endregion


        #region Listeners
        private void OnGameEnd()
        {
            StopAllCoroutines();
            _canShoot = false;
        }

        private void OnPlayerScoreChanged(uint oldScore, uint newScore)
        {
            updatePlayerScoreAction.Execute();
        }
        #endregion

        #region NetworkCallbacks
        public override void OnNetworkSpawn()
        {
            
        }

        public override void OnNetworkDespawn()
        {
            _bullets.Clear();
            players.Remove(this);
        }
        #endregion
    }
}
