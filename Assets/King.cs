using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class King : PieceDATA
{
    int bounds;
    public King()
    {
        if (Board.shogiType == "mini")
            bounds = 5;
        else bounds = 9;

        if (color < 0) pieceRef.FlipVertically();
        return; // No UI bullshit since can't promote
    }

    public override void CheckPromotion(int row)
    {
        return; // King can't promote
    }

    public override List<(int, int)> GetLegalMoves()
    {
        List<(int, int)> moves = new List<(int, int)>();

        // Define possible move offsets for a King (relative to its position)
        int[,] moveOffsets = new int[,]
        {
        { -1, -1 }, { -1, 0 }, { -1, 1 }, // Forward moves 
        {  0, -1 }, {  0, 1 },           // Side moves
        { 1, -1 }, { 1, 0 }, { 1, 1 }  // Backward moves
        };

        // Try every movement direction
        for (int i = 0; i < 8; i++)
        {
            int newRow = row + moveOffsets[i, 0];
            int newCol = col + moveOffsets[i, 1];

            // Check if the new position is within the board bounds
            if (newRow >= 0 && newRow < bounds && newCol >= 0 && newCol < bounds)
            {
                int targetPiece = BoardArray[newRow, newCol];

                // Check if the square is empty or contains an opponent's piece
                if ((BoardArray[newRow, newCol] == 0) || ((color > 0 && BoardArray[newRow, newCol] < 0) || (color < 0 && BoardArray[newRow, newCol] > 0)))
                {
                    moves.Add((newRow, newCol));
                }
            }
        }
        return moves;
    }  
}
