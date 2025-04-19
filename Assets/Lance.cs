using System.Collections.Generic;
using UnityEngine;

public class Lance : PieceDATA
{
    private int bounds;

    public override List<(int, int)> GetLegalMoves()
    {
        if (Board.shogiType == "mini")
            bounds = 5;
        else bounds = 9;

        if (!promoted)
        {
            if ((color == 1 && row == 0) || (color == -1 && row == bounds-1)) // Last rank
            {
                pieceRef.Promote(); // Force promotion
                return null;
            }
            else
                return LanceMove();
        }

        else
            return GoldGeneralMove();
    }

    public List<(int, int)> LanceMove()
    {
        List<(int, int)> moves = new List<(int, int)>();
        (int row, int col) move;

       // Debug.Log($"LanceMove: Starting for piece at ({row}, {col}), color: {color}, board bounds: {bounds}");

        // White pieces forward direction is --
        if (color == 1)
        {
           // Debug.Log("LanceMove: Processing White piece (upward movement)");
            for (int i = row-1; i > -1; i--) // Look forward
            {
               // Debug.Log($"LanceMove: Checking position ({i}, {col})");
                // Empty square
                if (BoardArray[i, col] == 0)
                {
                    move.row = i; move.col = col;
                    moves.Add(move);
                   // Debug.Log($"LanceMove: Added empty square move to ({i}, {col})");
                }
                // Capture
                else if ((color > 0 && BoardArray[i, col] < 0) || (color < 0 && BoardArray[i, col] > 0) && BoardArray[i, col] != 0)
                {
                    move.row = i; move.col = col;
                    moves.Add(move);
                    //Debug.Log($"LanceMove: Added capture move to ({i}, {col}), enemy piece: {BoardArray[i, col]}");
                    break; // Enemy piece ends line of sight
                }
                // Friendly piece
                else
                {
                    //Debug.Log($"LanceMove: Stopped at ({i}, {col}), friendly piece or invalid: {BoardArray[i, col]}");
                    break; // illegal move + ends line of sight
                }
            }
        }
        // Black pieces forward direction is ++
        else
        {
            //Debug.Log("LanceMove: Processing Black piece (downward movement)");
            for (int i = row+1; i < bounds; i++) // Look forward
            {
                //Debug.Log($"LanceMove: Checking position ({i}, {col})");
                // Empty square
                if (BoardArray[i, col] == 0)
                {
                    move.row = i; move.col = col;
                    moves.Add(move);
                    //Debug.Log($"LanceMove: Added empty square move to ({i}, {col})");
                }
                // Capture
                else if ((color > 0 && BoardArray[i, col] < 0) || (color < 0 && BoardArray[i, col] > 0) && BoardArray[i, col] != 0)
                {
                    move.row = i; move.col = col;
                    moves.Add(move);
                   // Debug.Log($"LanceMove: Added capture move to ({i}, {col}), enemy piece: {BoardArray[i, col]}");
                    break; // Enemy piece ends line of sight
                }
                // Friendly piece
                else
                {
                    //Debug.Log($"LanceMove: Stopped at ({i}, {col}), friendly piece or invalid: {BoardArray[i, col]}");
                    break; // illegal move + ends line of sight
                }
            }
        }

        //Debug.Log($"LanceMove: Returning {moves.Count} moves: {string.Join(", ", moves)}");
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
