using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DroppedPiece : MonoBehaviour, IPointerClickHandler
{
    private SpriteRenderer spr;
    public DroppedPieceDATA data;
    [SerializeField] Sprite sp;

    private void Awake()
    {
        spr = GetComponent<SpriteRenderer>();
    }

    public DroppedPiece Copy()
    {
        GameObject dropObject = Instantiate(data.boardRef.DroppablePiece);
        DroppedPiece copy = dropObject.GetComponent<DroppedPiece>();
        copy.data = new DroppedPieceDATA
        {
            color = this.data.color,
            pieceType = this.data.pieceType,
            promoted = this.data.promoted,
            boardRef = this.data.boardRef
        };
        return copy;
    }

    public void Init(Board board, int pieceType, int pieceColor, Sprite defaultSPR, bool promoted)
    {
        spr.sprite = defaultSPR;
        this.sp = defaultSPR;
        data.pieceType = pieceType;
        data.color = pieceColor;
        data.promoted = promoted;
        data.boardRef = board;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (data.color == ShogiGame.Instance.color)
        {
            data.boardRef.LastPieceClicked = gameObject;
            HighLightManager.ShowHighlights(data.GenerateMoves(), true);
        }
        else
        {
            Debug.Log("Piece Not Clickable");
        }
    }

    public void FlipVertically()
    {
        Vector2 offset = new Vector2(0f, 2.7f);
        if (spr != null)
            spr.flipY = true;
        BoxCollider2D Box2D = GetComponent<BoxCollider2D>();
        if (Box2D != null)
            Box2D.offset += offset;
    }

    public void ResetCollider()
    {
        if (gameObject.GetComponent<BoxCollider2D>() != null)
            Destroy(GetComponent<BoxCollider2D>());

        gameObject.AddComponent<BoxCollider2D>();
    }
}