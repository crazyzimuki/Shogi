using System.Collections.Generic;
using UnityEngine;

public abstract class PieceDATA
{
    public int gamebounds;
    public Board boardRef;           // Live board reference
    public BoardDATA simulationBoardData;  // Simulation board pointer
    //public bool simulatedCapture;
    public Piece pieceRef;
    public static int nextPieceId = 0;
    public bool isClickable;
    public bool isPromotionRank;
    public int row;
    public int col;
    public int color;
    public int pieceType;
    public int pieceId;
    public bool promoted;
    public bool doublepromoted; // Only for pieces that can double promote (chu-shogi)
    public bool triplepromoted; // Only for pieces that can triple promote (chu-shogi)
    public int[,] BoardArray
    {
        get
        {
            if (boardRef != null && boardRef.data != null)
                return boardRef.data.board;
            else if (simulationBoardData != null)
                return simulationBoardData.board;
            else
                return null;
        }
    }

    public abstract List<(int, int)> GetLegalMoves();

    public virtual void CheckPromotion(int rowBeforeMove)
    {
        pieceRef.CheckPromotionRank();
        if (!promoted)
        {
            if (Board.shogiType == "mini")
            {
                // player moving into promotion rank
                if ((color == 1 && row == 0) || (color == -1 && row == 4))
                    pieceRef.UIPromotion();

                // player moving out of promotion rank
                if ((color == 1 && rowBeforeMove == 0 && row != 0) || (color == -1 && rowBeforeMove == 4 && row != 4))
                    pieceRef.UIPromotion();
            }
            else if (Board.shogiType == "chu") // Chu-shogi
            {
                // player moving into promotion area
                if ((color == 1 && row < 4) || (color == -1 && row > 7))
                    pieceRef.UIPromotion();

                // player moving out of promotion area
                if ((color == 1 && rowBeforeMove < 4 && row > 3) || (color == -1 && rowBeforeMove > 6 && row < 7))
                    pieceRef.UIPromotion();
            }
            else // Regular shogi
            {               
                // player moving into promotion area
                if ((color == 1 && row < 3) || (color == -1 && row > 5))
                    pieceRef.UIPromotion();

                // player moving out of promotion area
                if ((color == 1 && rowBeforeMove < 3 && row > 2) || (color == -1 && rowBeforeMove > 5 && row < 6))
                    pieceRef.UIPromotion();

            }
        }
    }

    public void UpdatePosition(int newRow, int newCol)
    {
        row = newRow;
        col = newCol;
    }

    public virtual PieceDATA Copy(Board b)
    {
        PieceDATA copy = CreatePieceByType(pieceType);
        if (copy != null)
        {
            copy.pieceId = pieceId;
            copy.row = row;
            copy.col = col;
            copy.color = color;
            copy.pieceType = pieceType;
            copy.promoted = promoted;
            //copy.simulatedCapture = simulatedCapture;
            copy.boardRef = b;
        }
        return copy;
    }

    public void Print()
    {
        Debug.Log($"Piece {pieceType} of Color {color} at ({row},{col})");
    }

    public static PieceDATA CreatePieceByType(int pieceType)
    {
        switch (pieceType)
        {
            case 9: return new Lance();
            case 8: return new Horse();
            case 7: return new King();
            case 5: return new Rook();
            case 4: return new Bishop();
            case 3: return new Gold();
            case 2: return new Silver();
            case 1: return new Pawn();
            default: return new Pawn();
        }
    }

    public PieceDATA ShallowCopy()
    {
        return (PieceDATA)MemberwiseClone();
    }
}