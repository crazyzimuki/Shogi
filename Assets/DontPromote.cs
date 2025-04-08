using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DontPromote : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Piece parentPiece;

    private void Awake()
    {
        parentPiece = GetComponentInParent<Piece>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        parentPiece.DontPromote();
    }

    private void OnDestroy()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
