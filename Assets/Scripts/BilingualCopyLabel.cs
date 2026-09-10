using TMPro;
using UnityEngine;

public sealed class BilingualCopyLabel:MonoBehaviour
{
    [SerializeField] private string turkish,english;
    public void Configure(string tr,string en){turkish=tr;english=en;Refresh();}
    private void OnEnable(){GameLanguageService.Changed+=Refresh;Refresh();}
    private void OnDisable(){GameLanguageService.Changed-=Refresh;}
    private void Refresh(){var label=GetComponent<TMP_Text>();if(label!=null)label.text=GameContentCopy.Text(turkish,english);}
}
