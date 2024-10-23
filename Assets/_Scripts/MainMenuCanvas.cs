using UnityEngine;
using Fishing.Network;
using UnityEngine.UI;
using TMPro;
using Fishing.Variables;

public class MainMenuCanvas : MonoBehaviour
{
    [SerializeField] private GameObject mainPanel, loadingPanel;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private StringVariable playerNameVariable;
    [SerializeField] private bool isServer = false;
    private NetworkHandler _networkHandler;

    private void OnEnable()
    {
        inputField.onValueChanged.AddListener(OnNameValueChanged);
    }

    private void OnDisable()
    {
        inputField.onValueChanged.RemoveListener(OnNameValueChanged);
    }

    private void Start()
    {
        _networkHandler = NetworkHandler.Instance;
        mainPanel.SetActive(true);
        loadingPanel.SetActive(false);
        startGameButton.interactable = false;
        if (isServer)
            StartAsServer();
    }

    private void OnNameValueChanged(string val)
    {
        startGameButton.interactable = val.Length > 0;
    }

    public void StartAsServer()
    {
        _networkHandler.StartServer();
        mainPanel.SetActive(false);
        loadingPanel.SetActive(true);
    }

    public void StartAsClient()
    {
        playerNameVariable.Set(inputField.text);
        _networkHandler.StartClient();
        mainPanel.SetActive(false);
        loadingPanel.SetActive(true);
    }
}
