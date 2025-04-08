using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Promote : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Piece parentPiece;

    private void Awake()
    {
        parentPiece = GetComponentInParent<Piece>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        parentPiece.Promote();
    }

    private void OnDestroy()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
