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
        public Action<List<bool>> lastTenBulletResultsUpdated;

        [SerializeField] private FishListVariable fishes;

        private List<PlayerBullet> _activeBullets = new List<PlayerBullet>();
        private List<bool> _lastTenResults = new List<bool>();

        public override void Initialize()
        {
            _activeBullets.Clear();
            _lastTenResults.Clear();
        }

        public override void OnDestroy()
        {
            _activeBullets.Clear();
            _lastTenResults.Clear();
        }

        private void EnsureHitProbability()
        {
            if (_activeBullets.Count == 0)
                return;

            if (_lastTenResults.Count >= 7 && _lastTenResults.Where(b => b).Count() < 3)
            {
                var bullet = _activeBullets[0];
                var activeFishes = fishes.value.Where(f => f.IsActive).ToList();
                var closestFishes = activeFishes.OrderBy(f => Vector2.Distance(f.transform.position, bullet.transform.position)).ToList();

                if (closestFishes.Count > 0)
                {
                    var closestFish = closestFishes.First();
                    bullet.SetTarget(closestFish);
                }
            }
        }

        public void AddActiveBullet(PlayerBullet bullet)
        {
            _activeBullets.Add(bullet);
            EnsureHitProbability();
        }

        public void AddBulletResult(uint bulletId, bool hasScoredAHit)
        {
            var bullet = _activeBullets.FirstOrDefault(b => b.BulletId == bulletId);
            if (bullet) _activeBullets.Remove(bullet);

            if (_lastTenResults.Count >= 10)
            {
                int extra = _lastTenResults.Count - 9;
                for (int i = 0; i < extra; i++) _lastTenResults.RemoveAt(i);
            }

            _lastTenResults.Add(hasScoredAHit);
            lastTenBulletResultsUpdated?.Invoke(_lastTenResults);

            EnsureHitProbability();
        }
    }
}
