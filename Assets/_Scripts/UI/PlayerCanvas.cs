using UnityEngine;
using TMPro;

namespace Fishing.UI
{
    public class PlayerCanvas : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameTMP;
        [SerializeField] private TMP_Text scoreTMP;
        [SerializeField] private float offsetY;

        public void UpdatePosition(Vector3 referencePosition)
        {
            Vector3 newPosition = referencePosition;
            newPosition.y += newPosition.y < 0 ? offsetY : -offsetY;
            transform.position = newPosition;
        }

        public void UpdateNameAndScore(string name, string score)
        {
            nameTMP.text = name;
            scoreTMP.text = score;
        }
    }
}
