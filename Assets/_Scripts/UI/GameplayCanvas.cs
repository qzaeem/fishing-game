using UnityEngine;
using UnityEngine.UI;
using Fishing.Variables;
using Fishing.Gameplay;
using Fishing.Actions;
using System.Linq;
using TMPro;
using System.Collections.Generic;

namespace Fishing.UI
{
    public class GameplayCanvas : MonoBehaviour
    {
        [Header("Fields")]
        [SerializeField] private Color bulletMissColor, bulletHitColor;
        [SerializeField] private TMP_Text localPlayerNameTMP, localPlayerScoreTMP, otherPlayerNameTMP, otherPlayerScoreTMP, timerTMP;
        [SerializeField] private List<Image> bulletImages = new List<Image>();

        [Header("Game End")]
        [SerializeField] private GameObject gameEndPanel;
        [SerializeField] private TMP_Text player1NameTMP;
        [SerializeField] private TMP_Text player2NameTMP;
        [SerializeField] private TMP_Text player1ScoreTMP;
        [SerializeField] private TMP_Text player2ScoreTMP;

        [Header("Scriptable Objects")]
        [SerializeField] private PlayersListVariable players;
        [SerializeField] private StringVariable playerNameVariable;
        [SerializeField] private BulletsManager bulletsManager;
        [SerializeField] private IntVariable timerVariable;
        [SerializeField] private ActionSO updateScoreAction;
        [SerializeField] private ActionSO gameEndAction;

        private PlayerManager _owner;

        private void OnEnable()
        {
            updateScoreAction.executeAction += OnUpdateScore;
            bulletsManager.lastTenBulletResultsUpdated += OnBulletsResultUpdated;
            gameEndAction.executeAction += OnGameEnd;
            timerVariable.onValueChange += OnTimerUpdate;
        }

        private void OnDisable()
        {
            updateScoreAction.executeAction -= OnUpdateScore;
            bulletsManager.lastTenBulletResultsUpdated -= OnBulletsResultUpdated;
            gameEndAction.executeAction += OnGameEnd;
            timerVariable.onValueChange += OnTimerUpdate;
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _owner = players.value.FirstOrDefault(p => p.LocalClientIsOwner());
            var otherPlayer = players.value.FirstOrDefault(p => !p.LocalClientIsOwner());
            localPlayerNameTMP.text = playerNameVariable.value;

            if (otherPlayer)
                otherPlayerNameTMP.text = otherPlayer.playerName.Value.ToString();
            else
            {
                otherPlayerNameTMP.gameObject.SetActive(false);
                otherPlayerScoreTMP.gameObject.SetActive(false);
            }

            gameEndPanel.SetActive(false);
            timerTMP.text = "00";
        }

        private void OnUpdateScore()
        {
            players.value.ForEach(p =>
            {
                if (p.IsLocalPlayer)
                {
                    localPlayerScoreTMP.text = p.score.Value.ToString();
                }
                else
                {
                    otherPlayerScoreTMP.text = p.score.Value.ToString();
                    otherPlayerNameTMP.text = p.playerName.Value.ToString();
                }
            });

            otherPlayerNameTMP.gameObject.SetActive(players.value.Count > 1);
            otherPlayerScoreTMP.gameObject.SetActive(players.value.Count > 1);
        }

        private void OnBulletsResultUpdated(List<bool> results)
        {
            int bulletImageIndex = 0;

            for(int i = results.Count - 1; i >= 0; i--)
            {
                bulletImages[bulletImageIndex].color = results[i] ? bulletHitColor : bulletMissColor;
                bulletImageIndex++;
            }
        }

        private void OnTimerUpdate(int val)
        {
            timerTMP.text = val.ToString("00");
        }

        private void OnGameEnd()
        {
            player1NameTMP.text = _owner.playerName.Value.ToString();
            player1ScoreTMP.text = _owner.score.Value.ToString();

            var otherPlayer = players.value.FirstOrDefault(p => !p.IsLocalPlayer);

            if (otherPlayer)
            {
                player2NameTMP.text = otherPlayer.playerName.Value.ToString();
                player2ScoreTMP.text = otherPlayer.score.Value.ToString();
            }
            else
            {
                player2NameTMP.transform.parent.gameObject.SetActive(false);
            }

            gameEndPanel.SetActive(true);
        }
    }
}
