using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroppedPieceDATA
{
    private int bounds;
    public GameObject parent;
    public Board boardRef;
    public int pieceType;
    public int color;
    public bool promoted;

    public List<(int, int)> GenerateMoves()
    {
        if (Board.shogiType == "mini")
            bounds = 5;
        else bounds = 9;

        List<(int row, int col)> AllMoves = new List<(int row, int col)>();
        (int row, int col) move;

        if (boardRef == null)
            return AllMoves;

        for (int i = 0; i < bounds; i++)
        {
            for (int k = 0; k < bounds; k++)
            {
                if (boardRef.data.board[i, k] == 0) // empty square to drop to
                {
                    if (pieceType == 1) // drop is a pawn
                    {
                        if ((color == 1 && i != 0) || (color == -1 && i != bounds-1)) // Not last rank (illegal drop)
                        {
                            if (CheckCol(k)) // Check that column is free of your own pawns
                            {
                                move = (i, k);
                                AllMoves.Add(move);
                            }
                        }
                    }
                    else if (pieceType == 8) // Horse
                    {
                        if ((color == 1 && i > 1) || (color == -1 && i < 7)) // Not last two ranks (illegal drop)
                        {
                            move = (i, k);
                            AllMoves.Add(move);
                        }
                    }
                    else if (pieceType == 9) // Lance
                    {
                        if ((color == 1 && i != 0) || (color == -1 && i != bounds - 1)) // Not last rank (illegal drop)                        {
                        {
                            move = (i, k);
                            AllMoves.Add(move);
                        }
                    }
                    else // No further restrictions on drops
                    {
                        move = (i, k);
                        AllMoves.Add(move);
                    }
                }
            }
        }
        return AllMoves;
    }

    public bool CheckCol(int col)
    {
        for (int j = 0; j < bounds; j++)
        {
            if (boardRef.PieceAt(j, col) != null)
            {
                var PieceAtCol = boardRef.PieceAt(j, col).data;
                if (PieceAtCol.color == color && PieceAtCol.pieceType == 1 && PieceAtCol.promoted == false) // If we find specifically an unpromoted friendly pawn
                    return false;
            }
        }

        return true;
    }

    public void MakeSimulatedDrop((int row, int col) move, Board b)
    {
        // Place the piece on the board
        b.data.ModifyBoard(move.row, move.col, pieceType, color);
        // Create a new PieceDATA for the dropped piece with specific type
        PieceDATA newPiece = PieceDATA.CreatePieceByType(pieceType);
        newPiece.row = move.row;
        newPiece.col = move.col;
        newPiece.color = color;
        newPiece.promoted = promoted;
        newPiece.simulationBoardData = b.data; // Link to simulation board
        newPiece.boardRef = null; // Clear live board reference for simulation
        newPiece.pieceRef = b.PieceAt(move.row, move.col);
        b.Pieces.Add(newPiece.pieceRef);

        // Remove this drop from droppedPiecesData
        DroppedPieceDATA toRemove = b.data.droppedPiecesData.Find(d => d.pieceType == pieceType && d.color == color);
        if (toRemove != null)
            b.data.droppedPiecesData.Remove(toRemove);
    }
}