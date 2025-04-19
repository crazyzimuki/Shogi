using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Silver : PieceDATA    
{
    private int bounds;

    public override List<(int, int)> GetLegalMoves()
    {
        if (Board.shogiType == "mini")
            bounds = 5;
        else bounds = 9;

        if (!promoted)
            return SilverGeneralMove();
        else return GoldGeneralMove();
    }

    public List<(int,int)> SilverGeneralMove()
    {
        List<(int, int)> moves = new List<(int, int)>();

        // Define possible move offsets for a Silver General (relative to its position)
        int[,] moveOffsets = new int[,]
        {
        { -color, -1 }, { -color, 0 }, { -color, 1 }, // Forward moves 
        {  color,  1 }, {color, -1 }  // Backward moves
        };

        for (int i = 0; i < 5; i++)
        {
            int newRow = row + moveOffsets[i, 0];
            int newCol = col + moveOffsets[i, 1];

            // Check if the new position is within the board bounds
            if (newRow >= 0 && newRow < bounds && newCol >= 0 && newCol < bounds)
            {
                // Check if the target square is empty or contains an opponent's piece
                if ((BoardArray[newRow, newCol] == 0) || ((color > 0 && BoardArray[newRow, newCol] < 0) || (color < 0 && BoardArray[newRow, newCol] > 0)))
                {
                    moves.Add((newRow, newCol));
                    // Debug.Log("Row == " + newRow + " Col == " + newCol + " Added to list");
                }
            }
        }
        return moves;
    }

    public List<(int, int)> GoldGeneralMove()
    {
        List<(int, int)> moves = new List<(int, int)>();

        // Define possible move offsets for a Gold General (relative to its position)
        int[,] moveOffsets = new int[,]
        {
        { -color, -1 }, { -color, 0 }, { -color, 1 }, // Forward moves 
        {  0, -1 },          {  0, 1 }, // Side moves
        {  color,  0 }  // Backward move
        };

        for (int i = 0; i < 6; i++)
        {
            int newRow = row + moveOffsets[i, 0];
            int newCol = col + moveOffsets[i, 1];

            // Check if the new position is within the board bounds
            if (newRow >= 0 && newRow < bounds && newCol >= 0 && newCol < bounds)
            {
                // Check if the target square is empty or contains an opponent's piece
                if ((BoardArray[newRow, newCol] == 0) || ((color > 0 && BoardArray[newRow, newCol] < 0) || (color < 0 && BoardArray[newRow, newCol] > 0)))
                {
                    moves.Add((newRow, newCol));
                    // Debug.Log("Row == " + newRow + " Col == " + newCol + " Added to list");
                }
            }
        }
        return moves;
    }
}
