using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Highlight : MonoBehaviour, IPointerClickHandler
{
    public (int row, int col) move;
    public Board boardRef;
    public Piece parentPiece;
    public bool isDrop;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDrop)
            MakeDropMove();
        else if ((parentPiece.data.color == 1 && boardRef.data.board[move.row, move.col] > 0) ||
                 (parentPiece.data.color == -1 && boardRef.data.board[move.row, move.col] < 0))
        {
            Debug.Log("Illegal move: cannot capture own piece.");
            return;
        }
        else if (parentPiece != null)
        {
            if (ShogiGame.Instance.color == parentPiece.data.color)
                MakeMove();
        }
        else
        {
            Debug.LogError("Highlight clicked with null parentPiece.");
            return;
        }
    }

    public bool DisableMove(int color)
    {
        bool originalSim = ShogiGame.Instance.simulating;
        ShogiGame.Instance.simulating = true;

        if ((color == 1 && boardRef.data.board[move.row, move.col] > 0) ||
            (color == -1 && boardRef.data.board[move.row, move.col] < 0))
        {
            ShogiGame.Instance.simulating = originalSim;
            return true;
        }

        BoardDATA simCopy = boardRef.data.Clone();
        PieceDATA pCopy = simCopy.Pieces.Find(p => p.pieceId == parentPiece.data.pieceId);
        if (pCopy == null)
        {
            ShogiGame.Instance.simulating = originalSim;
            return true;
        }
        simCopy.MovePiece(simCopy, pCopy, move.row, move.col);
        bool inCheck = simCopy.isCheck(color, simCopy.Pieces);
        ShogiGame.Instance.simulating = originalSim;
        return inCheck;
    }

    public void MakeMove()
    {
        boardRef.data.MovePiece(boardRef.data, parentPiece.data, move.row, move.col);
        parentPiece.MovePieceTransform();
        HighLightManager.ClearHighlights();
        parentPiece.data.CheckPromotion();
        if (boardRef.data.IsCheckMate(boardRef, -parentPiece.data.color))
            ShogiGame.EndLife(parentPiece.data.color);
        else if (!Piece.isPromotionUIActive)
            ShogiGame.EndTurn();
    }

    public void MakeDropMove()
    {
        DroppedPiece dp = boardRef.LastPieceClicked.GetComponent<DroppedPiece>();
        boardRef.data.ModifyBoard(boardRef.data, move.row, move.col, dp.data.pieceType, dp.data.color);
        boardRef.SpawnPieceType(dp.data.pieceType, move.row, move.col);
        boardRef.data.droppedPiecesData.Remove(dp.data);
        HighLightManager.ClearHighlights();
        Piece p = boardRef.PieceAt(move.row, move.col);
        Destroy(boardRef.LastPieceClicked.gameObject);
        boardRef.data.droppedPiecesData.Remove(dp.data); // Remove from data list
        if (boardRef.LastPieceClicked != null && p != null)
            boardRef.LastPieceClicked = p.gameObject;
        if (boardRef.data.IsCheckMate(boardRef, -p.data.color))
            ShogiGame.EndLife(p.data.color == 1 ? 1 : -1);
        else
            ShogiGame.EndTurn();
    }

    public void MakeAIDropMove(DroppedPieceDATA drop)
    {
        boardRef.data.ModifyBoard(boardRef.data, move.row, move.col, drop.pieceType, -1);
        boardRef.SpawnPieceType(drop.pieceType, move.row, move.col);

        boardRef.data.droppedPiecesData.Remove(drop); // Remove from data list
        Destroy(drop.parent);

        if (boardRef.data.IsCheckMate(boardRef, -drop.color))
            ShogiGame.EndLife(drop.color == 1 ? 1 : -1);
        else
            ShogiGame.EndTurn();
    }
}