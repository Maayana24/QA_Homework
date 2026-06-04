using UnityEngine;

public class Torch : MonoBehaviour
{
    [SerializeField] GameObject lightSource;
    [SerializeField] MeshRenderer torchHead;
    [SerializeField] Material headOnMat;
    public void FlameOn()
    {
        lightSource.SetActive(true);
        torchHead.material = headOnMat;
    }
}
