using UnityEngine;
using Unity.Netcode;
using Fishing.Variables;
using System.Collections;

namespace Fishing.Gameplay
{
    public class Fish : NetworkBehaviour
    {
        public enum Volatility { Low = 20, Medium = 60, High = 100 }

        #region Network Variables
        public NetworkVariable<uint> fishID = new NetworkVariable<uint>(0);
        #endregion

        [SerializeField] private Volatility volatility;
        [SerializeField] private FishListVariable fishes;
        [SerializeField] private ConfigVariable gameConfig;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator myAnim;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Rigidbody2D rbody;
        [SerializeField] private float zPosition;
        [SerializeField] private float speedFactor = 1;
        [SerializeField] private float runFactor;
        [SerializeField] private float runMinWaitTime;
        [SerializeField] private float runMaxWaitTime;

        private Vector2 _screenBounds;
        private bool _movingRight;
        private int _currentHealth;
        private int _score = 1;
        private float _movingSpeed;

        public bool IsActive { get; set; }
        public int Score { get { return _score; } }


        void Start()
        {
            fishes.Add(this);

            if (volatility == Volatility.Low)
            {
                _movingSpeed = gameConfig.value.lowVolatilityFishSpeed;
                _score = gameConfig.value.lowVolatilityFishScore;
            }
            else if (volatility == Volatility.Medium)
            {
                _movingSpeed = gameConfig.value.mediumVolatilityFishSpeed;
                _score = gameConfig.value.mediumVolatilityFishScore;
            }
            else if (volatility == Volatility.High)
            {
                _movingSpeed = gameConfig.value.higholatilityFishSpeed;
                _score = gameConfig.value.highVolatilityFishScore;
            }

            // Assuming orthographic camera is at x = 0 and y = 0.
            float maxY = Camera.main.orthographicSize;
            _screenBounds = new Vector2((float)Screen.width / Screen.height * maxY, maxY);
            ResetFish();
        }

        void Update()
        {
            if (!IsServer || !IsActive)
                return;

            transform.position += transform.up * _movingSpeed * Time.deltaTime * speedFactor;

            if((_movingRight && transform.position.x > _screenBounds.x + 5)
                || (!_movingRight && transform.position.x < -_screenBounds.x - 5))
            {
                ResetFish();
            }
        }

        private IEnumerator RunCoroutine()
        {
            yield return new WaitForSeconds(Random.Range(runMinWaitTime, runMaxWaitTime));

            Run();
        }

        public void ResetFish()
        {
            if (!IsServer)
                return;

            _currentHealth = (int)volatility;
            StopAllCoroutines();
            ResetFishRpc();
        }

        public int FishHit(int damage)
        {
            _currentHealth -= damage;

            if(_currentHealth <= 0)
            {
                _currentHealth = 0;
                StopAllCoroutines();
                ResetFishRpc();
                return _score;
            }

            return 0;
        }

        public void Run()
        {
            RunRpc();
        }

        public void ActivateFish()
        {
            int dir = Random.Range(1, 3) == 1 ? 1 : -1;
            transform.position = new Vector3(_screenBounds.x * dir + dir * 2, Random.Range(-_screenBounds.y + 1, _screenBounds.y - 1), zPosition);
            transform.up = dir == 1 ? Vector3.left : Vector3.right;
            _movingRight = dir != 1;
            _currentHealth = (int)volatility;
            ActivateFishRpc();
            //StartCoroutine(RunCoroutine());
        }

        #region RPCs
        [Rpc(SendTo.Everyone, RequireOwnership = true)]
        private void ResetFishRpc()
        {
            spriteRenderer.enabled = false;
            IsActive = false;
            rbody.bodyType = RigidbodyType2D.Kinematic;
            myAnim.enabled = false;
            myAnim.SetFloat("speed", 0);
            bodyCollider.enabled = false;
            speedFactor = 1;
            transform.position = Vector3.zero + Vector3.down * 20;
        }

        [Rpc(SendTo.Everyone, RequireOwnership = true)]
        private void ActivateFishRpc()
        {
            IsActive = true;
            rbody.bodyType = RigidbodyType2D.Dynamic;
            myAnim.enabled = true;
            myAnim.SetFloat("speed", 1);
            spriteRenderer.enabled = true;
            bodyCollider.enabled = true;
            speedFactor = 1;
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void RunRpc()
        {
            speedFactor = runFactor;
            myAnim.SetFloat("speed", runFactor);
        }
        #endregion
    }
}
