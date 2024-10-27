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
        [SerializeField] private TMP_Text localPlayerNameTMP;
        [SerializeField] private TMP_Text localPlayerScoreTMP;
        [SerializeField] private TMP_Text otherPlayerNameTMP;
        [SerializeField] private TMP_Text otherPlayerScoreTMP;
        [SerializeField] private TMP_Text timerTMP;
        [SerializeField] private Image bulletImagePrefab;
        [SerializeField] private Transform bulletImagesContainer;

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
        [SerializeField] private ConfigVariable gameConfig;

        private Color lowFishColor;
        private Color mediumFishColor;
        private Color highFishColor;
        private Color noHitColor;
        private int bulletsPerSecond;
        private PlayerManager _owner;
        private List<Image> bulletImages = new List<Image>();

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

            lowFishColor = gameConfig.value.lowFishColor;
            mediumFishColor = gameConfig.value.mediumFishColor;
            highFishColor = gameConfig.value.highFishColor;
            noHitColor = gameConfig.value.noHitColor;

            bulletsPerSecond = gameConfig.value.bulletsPerSecond;

            gameEndPanel.SetActive(false);
            timerTMP.text = "00";
            PopulateBulletImages();
        }

        private void PopulateBulletImages()
        {
            bulletImages.Clear();

            for(int i = 0; i < bulletsPerSecond; i++)
            {
                var bulletImage = Instantiate(bulletImagePrefab, bulletImagesContainer);
                bulletImage.GetComponentInChildren<TMP_Text>().text = "";
                bulletImages.Add(bulletImage);
            }
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

        private void OnBulletsResultUpdated(List<int> results)
        {
            int bulletImageIndex = 0;

            for(int i = results.Count - 1; i >= 0; i--)
            {
                Color color = noHitColor;
                if (results[i] == 1)
                    color = lowFishColor;
                else if (results[i] == 3)
                    color = mediumFishColor;
                else if (results[i] == 7)
                    color = highFishColor;

                bulletImages[bulletImageIndex].color = color;
                bulletImages[bulletImageIndex].GetComponentInChildren<TMP_Text>().text = results[i].ToString();
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
