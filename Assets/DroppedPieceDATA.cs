using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroppedPieceDATA
{
    public GameObject parent;
    public Board boardRef;
    public int pieceType;
    public int color;
    public bool promoted;

    public List<(int, int)> GenerateMoves()
    {
        List<(int row, int col)> AllMoves = new List<(int row, int col)>();
        (int row, int col) move;

        if (boardRef == null)
            return AllMoves;

        for (int i = 0; i < 5; i++)
        {
            for (int k = 0; k < 5; k++)
            {
                if (boardRef.data.board[i, k] == 0)
                {
                    if (pieceType == 1)
                    {
                        if ((color == 1 && i != 0) || (color == -1 && i != 4))
                        {
                            if (CheckCol(k))
                            {
                                move = (i, k);
                                AllMoves.Add(move);
                            }
                        }
                    }
                    else
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
        for (int j = 0; j < 5; j++)
        {
            if (Mathf.Abs(boardRef.data.board[j, col]) == 1)
                return false;
        }
        return true;
    }

    public void MakeSimulatedDrop((int row, int col) move, Board b)
    {
        // Place the piece on the board
        b.data.ModifyBoard(b.data, move.row, move.col, pieceType, color);
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