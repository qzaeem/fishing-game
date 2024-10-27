using UnityEngine;
using Unity.Netcode;
using Fishing.Variables;
using System.Linq;
using System.Collections;

namespace Fishing.Gameplay
{
    public class PlayerBullet : NetworkBehaviour
    {
        #region Networked Variables

        #endregion

        [Header("Fields")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Rigidbody2D rbody;
        [SerializeField] private float zPosition;

        [Header("Attributes")]
        [SerializeField] private float shootSpeed;
        [SerializeField] private float bulletLife;
        [SerializeField] private int damage;

        [Header("Scriptable Object Variables")]
        [SerializeField] private PlayersListVariable players;
        [SerializeField] private HitFishVariable hitFishVariable;

        private Vector2 _direction;
        private Vector2 _screenBounds;
        private float _bulletRadius; // This bullet's radius in world units.
        private PlayerManager _owner;
        private Fish _target;

        public bool IsActive { get; set; }
        public uint BulletId { get; set; }
        public int CurrentDamage { get; set; }

        private void Start()
        {
            var player = players.value.FirstOrDefault(p => p.OwnerClientId == OwnerClientId);

            if (player)
            {
                player.AddBullet(this);
                _owner = player;
            }

            // Assuming orthographic camera is at x = 0 and y = 0.
            float maxY = Camera.main.orthographicSize;
            _screenBounds = new Vector2((float)Screen.width / Screen.height * maxY, maxY);

            // Assuming bullet is of radius 1 when its scale is set to (1, 1).
            _bulletRadius = transform.localScale.x;

            if (!this.LocalClientIsOwner())
                return;

            ResetBullet();
        }

        private void Update()
        {
            if (!IsActive || !this.LocalClientIsOwner())
                return;


            if (_target && _target.IsActive)
            {
                Vector3 targetVector = _target.transform.position - transform.position;

                if (Vector2.Angle(_direction, targetVector) > 0.25f)
                {
                    _direction = Vector3.RotateTowards(_direction, targetVector, Time.deltaTime * 50, 0);
                    _direction = _direction.normalized;
                }
            }


            var nextPosition = (Vector2)transform.position + _direction * shootSpeed * Time.deltaTime;
            float dx = 0, dy = 0;

            if(nextPosition.x + _bulletRadius > _screenBounds.x)
            {
                dx = nextPosition.x + _bulletRadius - _screenBounds.x;
                float angle = Vector2.Angle(Vector2.left, -_direction);
                float dist = dx / (Mathf.Cos(angle) * Mathf.Rad2Deg);
                nextPosition -= _direction * dist;
            }
            else if (nextPosition.x - _bulletRadius < -_screenBounds.x)
            {
                dx = _bulletRadius - nextPosition.x - _screenBounds.x;
                float angle = Vector2.Angle(Vector2.right, -_direction);
                float dist = dx / (Mathf.Cos(angle) * Mathf.Rad2Deg);
                nextPosition -= _direction * dist;
            }

            if (nextPosition.y + _bulletRadius > _screenBounds.y)
            {
                dy = nextPosition.y + _bulletRadius - _screenBounds.y;
                float angle = Vector2.Angle(Vector2.down, -_direction);
                float dist = dy / (Mathf.Cos(angle) * Mathf.Rad2Deg);
                nextPosition -= _direction * dist;
            }
            else if (nextPosition.y - _bulletRadius < -_screenBounds.y)
            {
                dy = _bulletRadius - nextPosition.y - _screenBounds.y;
                float angle = Vector2.Angle(Vector2.up, -_direction);
                float dist = dy / (Mathf.Cos(angle) * Mathf.Rad2Deg);
                nextPosition -= _direction * dist;
            }

            nextPosition.x = Mathf.Clamp(nextPosition.x, -_screenBounds.x + _bulletRadius, _screenBounds.x - _bulletRadius);
            nextPosition.y = Mathf.Clamp(nextPosition.y, -_screenBounds.y + _bulletRadius, _screenBounds.y - _bulletRadius);
            transform.position = new Vector3(nextPosition.x, nextPosition.y, zPosition);

            dx = Mathf.Floor(dx * 100f) / 100f;
            dy = Mathf.Floor(dy * 100f) / 100f;

            if (dx > dy)
            {
                _direction.x = -_direction.x;
            }
            if (dy > dx)
            {
                _direction.y = -_direction.y;
            }
            else if(dx > 0 && dy > 0)
            {
                _direction.x = -_direction.x;
                _direction.y = -_direction.y;
            }
        }

        private IEnumerator BulletLifeCoroutine()
        {
            yield return new WaitForSeconds(bulletLife);
            _owner.AddBulletResult(BulletId, 0);
            ResetBullet();
        }

        private void ResetBullet()
        {
            StopAllCoroutines();
            ResetBulletRpc();
            _target = null;
        }

        public void Shoot(Vector3 spawnPoint, Vector2 direction)
        {
            if (IsActive)
                return;

            _target = null;
            _direction = direction;
            transform.position = spawnPoint;
            CurrentDamage = Random.Range(damage, (damage * 4) + 1);
            ShootBulletRpc(spawnPoint, direction);
            //StartCoroutine(BulletLifeCoroutine()); // Bullet will only be destroyed after hitting a fish.
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if(collision.tag == "fish")
            {
                spriteRenderer.enabled = false;
                trailRenderer.emitting = false;
                trailRenderer.enabled = false;
                bodyCollider.enabled = false;

                if (!this.LocalClientIsOwner())
                    return;

                var fish = collision.GetComponent<Fish>();
                if (!fish.IsActive) return;
                hitFishVariable.Set(new HitFishParams { fishId = fish.fishID.Value, bulletId = BulletId, damage = (uint)CurrentDamage });
                ResetBullet();
            }
        }

        public void SetTarget(Fish target)
        {
            _target = target;
            StopAllCoroutines();
            StartCoroutine(BulletLifeCoroutine());
        }

        #region RPCs
        [Rpc(SendTo.Everyone)]
        private void ResetBulletRpc()
        {
            spriteRenderer.enabled = false;
            trailRenderer.emitting = false;
            trailRenderer.enabled = false;
            bodyCollider.enabled = false;
            IsActive = false;
            CurrentDamage = damage;
            rbody.angularVelocity = 0;
            rbody.linearVelocity = Vector2.zero;
            rbody.bodyType = RigidbodyType2D.Kinematic;

            var owner = players.value.FirstOrDefault(p => p.OwnerClientId == OwnerClientId);

            if (owner)
            {
                transform.position = owner.transform.position;
            }
        }

        [Rpc(SendTo.Everyone)]
        private void ShootBulletRpc(Vector3 spawnPoint, Vector2 direction)
        {
            spriteRenderer.enabled = true;
            trailRenderer.enabled = true;
            trailRenderer.emitting = true;
            bodyCollider.enabled = true;
            IsActive = true;
            rbody.bodyType = RigidbodyType2D.Kinematic;
        }
        #endregion

        #region NetworkCallbacks
        public override void OnNetworkSpawn()
        {

        }

        public override void OnNetworkDespawn()
        {

        }
        #endregion
    }
}
