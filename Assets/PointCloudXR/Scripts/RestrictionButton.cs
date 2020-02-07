/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using UnityEngine;
using UnityEngine.EventSystems;

public class RestrictionButton : MonoBehaviour, ISelectHandler// required interface when using the OnSelect method.
{
    public ViveCtrl viveCtrl;
    public RestrictMode restrictMode;
    private bool isSelected = false;

   
    public void OnSelect(BaseEventData eventData)
    {
        Debug.Log(this.gameObject.name + " was selected");
    }
    
}
