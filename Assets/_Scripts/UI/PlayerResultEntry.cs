using UnityEngine;
using TMPro;

public class PlayerResultEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text nameTMP;
    [SerializeField] private TMP_Text scoreTMP;

    public void ShowResult(string name, string score)
    {
        nameTMP.text = name;
        scoreTMP.text = score;
    }
}
