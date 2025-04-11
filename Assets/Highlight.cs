using UnityEngine;
using UnityEngine.EventSystems;

public class Highlight : MonoBehaviour, IPointerClickHandler
{
    public (int row, int col) move;
    public Board boardRef;
    public Piece parentPiece;
    public bool isDrop;

    /// <summary>
    /// Handles clicks on highlights for moves or drops.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDrop)
        {
            MakeDropMove();
            return;
        }

        if (parentPiece == null)
        {
            Debug.LogError("Highlight clicked with null parentPiece.");
            return;
        }

        if (ShogiGame.Instance.color != parentPiece.data.color)
        {
            Debug.Log("Not your turn.");
            return;
        }

        if ((parentPiece.data.color == 1 && boardRef.data.board[move.row, move.col] > 0) ||
            (parentPiece.data.color == -1 && boardRef.data.board[move.row, move.col] < 0))
        {
            Debug.Log("Illegal move: cannot capture own piece.");
            return;
        }

        MakeMove();
    }

    /// <summary>
    /// Checks if a move should be disabled (e.g., captures own piece or leaves king in check).
    /// </summary>
    public bool DisableMove(int color)
    {
        if ((color == 1 && boardRef.data.board[move.row, move.col] > 0) ||
            (color == -1 && boardRef.data.board[move.row, move.col] < 0))
        {
            return true;
        }

        BoardDATA simCopy = boardRef.data.Clone();
        PieceDATA pCopy = simCopy.Pieces.Find(p => p.pieceId == parentPiece.data.pieceId);
        if (pCopy == null)
        {
            return true;
        }
        simCopy.MovePiece(pCopy, move.row, move.col);
        return simCopy.isCheck(color, simCopy.Pieces);
    }

    /// <summary>
    /// Performs a regular piece move and updates the game state.
    /// </summary>
    public void MakeMove()
    {
        int rowBeforeMove = parentPiece.data.row;
        boardRef.data.MovePiece(parentPiece.data, move.row, move.col);
        parentPiece.MovePieceTransform();
        parentPiece.data.CheckPromotion(rowBeforeMove);
        HighLightManager.ClearHighlights();
        ShogiGame.EndTurn();
    }

    /// <summary>
    /// Performs a drop move for the player and updates the game state.
    /// </summary>
    public void MakeDropMove()
    {
        DroppedPiece dp = boardRef.LastPieceClicked.GetComponent<DroppedPiece>();
        boardRef.data.ModifyBoard(move.row, move.col, dp.data.pieceType, dp.data.color);
        boardRef.SpawnPieceType(dp.data.pieceType, move.row, move.col);
        boardRef.data.droppedPiecesData.Remove(dp.data); // Remove only once
        HighLightManager.ClearHighlights();
        Piece p = boardRef.PieceAt(move.row, move.col);
        Destroy(boardRef.LastPieceClicked.gameObject);
        if (p != null)
            boardRef.LastPieceClicked = p.gameObject;
        ShogiGame.EndTurn();
    }

    /// <summary>
    /// Performs a drop move for the AI and updates the game state.
    /// </summary>
    public void MakeAIDropMove(DroppedPieceDATA drop)
    {
        boardRef.data.ModifyBoard(move.row, move.col, drop.pieceType, -1);
        boardRef.SpawnPieceType(drop.pieceType, move.row, move.col);
        boardRef.data.droppedPiecesData.Remove(drop);
        if (drop.parent != null)
            Destroy(drop.parent.gameObject);
        ShogiGame.EndTurn();
    }

    /// <summary>
    /// Checks if the game ends due to checkmate or ends the turn otherwise.
    /// ** MINIMAL FIX APPLIED HERE FOR CLARITY/ROBUSTNESS **
    /// </summary>
    private void CheckForGameEnd(int playerColor)
    {
        // First, check for game end condition (checkmate)
        if (boardRef.data.IsCheckMate(boardRef, -playerColor))
        {
            // If checkmate, end the game.
            ShogiGame.EndLife(playerColor);
        }
        // If the game did NOT end with checkmate, THEN check the promotion flag
        // to decide whether to end the turn now or defer it.
        else
        {
            // This is the crucial check: Only end the turn if the promotion UI
            // flag is NOT set to true.
            if (!Piece.isPromotionUIActive)
            {
                ShogiGame.EndTurn();
            }
            // Implicit else: If Piece.isPromotionUIActive IS true, do nothing here.
            // The turn ending responsibility falls to the promotion UI handler code.
            // (Adding a Debug.Log here can help trace the flow)
            // else { Debug.Log("CheckForGameEnd: Turn end deferred due to active promotion UI."); }
        }
    }
}