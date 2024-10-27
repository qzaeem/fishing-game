using UnityEngine;
using Fishing.Variables;
using Fishing.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fishing.Actions
{
    [CreateAssetMenu(fileName = "Bullets Manager", menuName = "Actions/Bullets Manager")]
    public class BulletsManager : ActionSO
    {
        public Action<List<int>> lastTenBulletResultsUpdated;

        [SerializeField] private FishListVariable fishes;
        [SerializeField] private ConfigVariable gameConfig;
        [SerializeField, Range(0, 1)] private float hitProbability;

        private List<PlayerBullet> _activeBullets = new List<PlayerBullet>();
        private List<int> _lastBulletSetResults = new List<int>();
        private int bulletsPerSecond;

        public override void Initialize()
        {
            _activeBullets.Clear();
            _lastBulletSetResults.Clear();
            bulletsPerSecond = gameConfig.value.bulletsPerSecond;
        }

        public override void OnDestroy()
        {
            _activeBullets.Clear();
            _lastBulletSetResults.Clear();
        }

        private void EnsureHitProbability()
        {
            if (_activeBullets.Count == 0)
                return;

            int probabilityBullets = (int)(bulletsPerSecond * hitProbability);
            if (_lastBulletSetResults.Count >= bulletsPerSecond - probabilityBullets && _lastBulletSetResults.Where(b => b > 0).Count() < probabilityBullets)
            {
                var bullet = _activeBullets[0];
                var activeFishes = fishes.value.Where(f => f.IsActive).ToList();
                //var closestFishes = activeFishes.OrderBy(f => Vector2.Distance(f.transform.position, bullet.transform.position)).ToList();

                //if (closestFishes.Count > 0)
                //{
                //    var closestFish = closestFishes.First();
                //    bullet.SetTarget(closestFish);
                //}

                bullet.CurrentDamage = 100;
            }
        }

        public void AddActiveBullet(PlayerBullet bullet)
        {
            _activeBullets.Add(bullet);
            EnsureHitProbability();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bulletId">The Id of the bullet.</param>
        /// <param name="hitScore">The score from that bullet.</param>
        public void AddBulletResult(uint bulletId, int hitScore)
        {
            var bullet = _activeBullets.FirstOrDefault(b => b.BulletId == bulletId);
            if (bullet) _activeBullets.Remove(bullet);

            if (_lastBulletSetResults.Count >= bulletsPerSecond)
            {
                int extra = _lastBulletSetResults.Count - (bulletsPerSecond - 1);
                for (int i = 0; i < extra; i++) _lastBulletSetResults.RemoveAt(i);
            }

            _lastBulletSetResults.Add(hitScore);
            lastTenBulletResultsUpdated?.Invoke(_lastBulletSetResults);

            EnsureHitProbability();
        }
    }
}
