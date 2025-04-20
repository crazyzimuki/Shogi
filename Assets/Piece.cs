using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Linq;

public class Piece : MonoBehaviour, IPointerClickHandler
{
    public PieceDATA data;
    [SerializeField] Sprite promotedSPR;
    public Sprite DefaultSPR;
    public SpriteRenderer spr;
    public GameObject PromotionUI;
    public static bool isPromotionUIActive;
    [SerializeField] Sprite[] UIimages;
    BoardGrid PromotionUIGrid;
    public Vector3 GridOffset;

    private void Awake()
    {
        spr = GetComponent<SpriteRenderer>();
    }

    public virtual void Start()
    {
        PromotionUI = GameObject.Find("PromotionUI");
        DefaultSPR = spr.sprite;
        UIimages = new Sprite[4];
        UIimages[1] = UIimages[2] = DefaultSPR;
        UIimages[3] = promotedSPR;
        if (data.color < 0) FlipVertically();
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

    public virtual void Promote()
    {
        data.promoted = true;
        spr.sprite = promotedSPR;
        Debug.Log("PIECE PROMOTED!");
        if (isPromotionUIActive)
        {
            PromotionUI.SetActive(false);
            isPromotionUIActive = false;
        }

        if (data.boardRef.data.IsCheckMate(data.boardRef, -data.color))
            ShogiGame.EndLife(data.color == 1 ? -1 : 1);

        ShogiGame.EndTurn();
    }

    public virtual void DontPromote()
    {
        data.promoted = false;
        if (isPromotionUIActive)
        {
            PromotionUI.SetActive(false);
            isPromotionUIActive = false;
        }

        if (data.boardRef.data.IsCheckMate(data.boardRef, -data.color))
            ShogiGame.EndLife(data.color == 1 ? -1 : 1);

        ShogiGame.EndTurn();
    }

    public virtual void UIPromotion()
    {
        if (Board.shogiType == "mini")
            data.gamebounds = 5;
        else data.gamebounds = 9;

        //// Forced promotions
        if (data.boardRef == null || data.color == -1)
        {
            Promote();
            ShogiGame.EndTurn();
            return;
        }

        if ((data.pieceType == 1 || data.pieceType == 9) && ((data.color == 1 && data.row == 0) || (data.color == -1 && data.row == data.gamebounds - 1))) // Pawn or Lance on last rank
        {
            Promote();
            ShogiGame.EndTurn();
            return;
        }

        if (data.pieceType == 8 && ((data.color == 1 && data.row < 2) || (data.color == -1 && data.row > 6))) // Horse on last two ranks
        {
            Promote();
            ShogiGame.EndTurn();
            return;
        }

        Vector3 SpawnPositionForBlack = new Vector3(-3f * .211f, 3f * .425f, 0f);
        if (data.color == 1)
            PromotionUI = Instantiate(PromotionUI, transform.position, Quaternion.identity, transform);
        else
            PromotionUI = Instantiate(PromotionUI, transform.localPosition + SpawnPositionForBlack, Quaternion.identity, transform);

        isPromotionUIActive = true;
        PromotionUI.SetActive(true);
        Vector3 localScale = new Vector3(4f, 4f, 1f);
        PromotionUI.transform.localScale = localScale;

        for (int i = 1; i < 4; i++)
        {
            PromotionUI.GetComponentsInChildren<SpriteRenderer>()[i].sprite = UIimages[i];
        }

        PromotionUI.transform.GetChild(2).localScale = new Vector3(0.3f, 0.28f, 1f);
        PromotionUI.transform.GetChild(0).transform.localPosition = new Vector3(0.0299999993f, 0.111000001f, 0f);
        PromotionUI.transform.GetChild(1).transform.localPosition = new Vector3(0.493000001f, -0.331999987f, 0f);
        PromotionUI.transform.GetChild(2).transform.localPosition = new Vector3(0.493000001f, 0.111000001f, 0f);

        BoxCollider2D[] colliders = PromotionUI.GetComponentsInChildren<BoxCollider2D>();
        foreach (BoxCollider2D col in colliders)
        {
            ResetCollider(col);
        }

        isPromotionUIActive = true;
    }

    public void ResetCollider(BoxCollider2D collider)
    {
        GameObject obj = collider.gameObject;
        Destroy(collider);
        BoxCollider2D newCol = obj.AddComponent<BoxCollider2D>();
        newCol.size = new Vector2(1f, 1.2f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (data.isClickable && !isPromotionUIActive)
        {
            data.boardRef.LastPieceClicked = gameObject;
            var legalMoves = data.GetLegalMoves();
            HighLightManager.ShowHighlights(legalMoves, false);
        }
        else
        {
            Debug.Log("Piece not clickable");
            return;
        }
    }

    public void MovePieceTransform()
    {
        if (transform == null)
            return;
        transform.position = HighLightManager.Grid.GetWorldPosition(data.col, data.row);

        transform.position += new Vector3(0.022f, data.color * 0.09f, 0f);

        if (data.color < 0)
        {
            if (ShogiGame.Instance.shogiType == "mini")
                transform.position += new Vector3(0f, -0.47f, 0f);
            else
                transform.localPosition += new Vector3(0f, -0.75f, 0f);
        }
    }

    public void CheckPromotionRank()
    {
        if (Board.shogiType == "mini")
        {
            if ((data.color == 1 && data.row == 0) || (data.color == -1 && data.row == 4))
                data.isPromotionRank = true;
            else
                data.isPromotionRank = false;
        }
        else // Regular shogi
        {
            if ((data.color == 1 && data.row < 3) || (data.color == -1 && data.row > 5))
                data.isPromotionRank = true;
            else
                data.isPromotionRank = false;
        }
    }
}