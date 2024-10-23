using UnityEngine;
using Unity.Netcode;
using Fishing.Variables;
using System.Collections;

namespace Fishing.Gameplay
{
    public class Fish : NetworkBehaviour
    {
        #region Network Variables
        public NetworkVariable<uint> fishID = new NetworkVariable<uint>(0);
        #endregion

        [SerializeField] private FishListVariable fishes;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator myAnim;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Rigidbody2D rbody;
        [SerializeField] private float zPosition;
        [SerializeField] private float speedFactor = 1;
        [SerializeField] private float runFactor;
        [SerializeField] private float movingSpeed;
        [SerializeField] private float runMinWaitTime;
        [SerializeField] private float runMaxWaitTime;
        [SerializeField] private int score = 1;

        private Vector2 _screenBounds;
        private bool _movingRight;

        public bool IsActive { get; set; }
        public int Score { get { return score; } }


        void Start()
        {
            fishes.Add(this);
            // Assuming orthographic camera is at x = 0 and y = 0.
            float maxY = Camera.main.orthographicSize;
            _screenBounds = new Vector2((float)Screen.width / Screen.height * maxY, maxY);
            ResetFish();
        }

        void Update()
        {
            if (!IsServer || !IsActive)
                return;

            transform.position += transform.up * movingSpeed * Time.deltaTime * speedFactor;

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

            StopAllCoroutines();
            ResetFishRpc();
        }

        public void FishHit()
        {
            StopAllCoroutines();
            ResetFishRpc();
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
            ActivateFishRpc();
            StartCoroutine(RunCoroutine());
        }

        #region RPCs
        [Rpc(SendTo.Everyone, RequireOwnership = true)]
        private void ResetFishRpc()
        {
            IsActive = false;
            rbody.bodyType = RigidbodyType2D.Kinematic;
            myAnim.enabled = false;
            myAnim.SetFloat("speed", 0);
            spriteRenderer.enabled = false;
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
