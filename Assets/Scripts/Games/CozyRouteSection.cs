using UnityEngine;

public static class CozyHomewardRules { public const float ClearApproachAt=66f; }
public sealed class CozyRouteSection:MonoBehaviour
{
    public GameObject[] districts;
    public void Select(int stage){if(districts==null)return;for(int i=0;i<districts.Length;i++)if(districts[i]!=null)districts[i].SetActive(i==Mathf.Clamp(stage,0,districts.Length-1));}
}
