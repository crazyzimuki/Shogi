using System.Collections.Generic;
using UnityEngine;

public class Chariot : PieceDATA
{
    private int bounds;

    public override List<(int, int)> GetLegalMoves()
    {
        if (Board.shogiType == "mini")
            bounds = 5;
        else if (Board.shogiType == "chu")
            bounds = 12;
        else bounds = 9;

        if (!promoted)
            return ChariotMove();
        else
            return WhaleMove();
    }

    public List<(int, int)> ChariotMove()
    {
        List<(int, int)> moves = new List<(int, int)>();
        (int row, int col) move;

        // Debug.Log($"ChariotMove: Starting for piece at ({row}, {col}), color: {color}, board bounds: {bounds}");

            // Debug.Log("ChariotMove: Processing piece (upward movement)");
            for (int i = row - 1; i > -1; i--) // Look forward
            {
                // Debug.Log($"ChariotMove: Checking position ({i}, {col})");
                // Empty square
                if (BoardArray[i, col] == 0)
                {
                    move.row = i; move.col = col;
                    moves.Add(move);
                    // Debug.Log($"ChariotMove: Added empty square move to ({i}, {col})");
                }
                // Capture
                else if ((color > 0 && BoardArray[i, col] < 0) || (color < 0 && BoardArray[i, col] > 0) && BoardArray[i, col] != 0)
                {
                    move.row = i; move.col = col;
                    moves.Add(move);
                    //Debug.Log($"ChariotMove: Added capture move to ({i}, {col}), enemy piece: {BoardArray[i, col]}");
                    break; // Enemy piece ends line of sight
                }
                // Friendly piece
                else
                {
                    //Debug.Log($"ChariotMove: Stopped at ({i}, {col}), friendly piece or invalid: {BoardArray[i, col]}");
                    break; // illegal move + ends line of sight
                }
            }

            //Debug.Log("ChariotMove: Processing piece (downward movement)");
            for (int i = row + 1; i < bounds; i++) // Look backward
            {
                //Debug.Log($"ChariotMove: Checking position ({i}, {col})");
                // Empty square
                if (BoardArray[i, col] == 0)
                {
                    move.row = i; move.col = col;
                    moves.Add(move);
                    //Debug.Log($"ChariotMove: Added empty square move to ({i}, {col})");
                }
                // Capture
                else if ((color > 0 && BoardArray[i, col] < 0) || (color < 0 && BoardArray[i, col] > 0) && BoardArray[i, col] != 0)
                {
                    move.row = i; move.col = col;
                    moves.Add(move);
                    // Debug.Log($"ChariotMove: Added capture move to ({i}, {col}), enemy piece: {BoardArray[i, col]}");
                    break; // Enemy piece ends line of sight
                }
                // Friendly piece
                else
                {
                    //Debug.Log($"ChariotMove: Stopped at ({i}, {col}), friendly piece or invalid: {BoardArray[i, col]}");
                    break; // illegal move + ends line of sight
                }
            }

        //Debug.Log($"ChariotMove: Returning {moves.Count} moves: {string.Join(", ", moves)}");
        return moves;
    }

    public List<(int, int)> WhaleMove()
    {
        List<(int, int)> moves = new List<(int, int)>();

        int newRow = row;
        int newCol = col;

        // Define infinite range move directions for a Whale (relative to its position)
        int[,] moveOffsets = new int[,]
{
        { -1, 0 }, // Forward moves 
        { 1, -1 }, { 1, 0 }, { 1, 1 }  // Backward moves
};

        for (int i = 0; i < 4; i++) // For each movement direction
        {
            // Reset values
            newRow = row;
            newCol = col;

            while (newRow >= 0 && newRow < bounds && newCol >= 0 && newCol < bounds) // Keep going until out of bounds
            {
                // Go in direction
                newRow += moveOffsets[i, 0];
                newCol += moveOffsets[i, 1];

                if (newRow >= 0 && newRow < bounds && newCol >= 0 && newCol < bounds) // If still in bounds
                {
                    bool isCapture = ((color > 0 && BoardArray[newRow, newCol] < 0) || (color < 0 && BoardArray[newRow, newCol] > 0) && BoardArray[newRow, newCol] != 0);
                    //Debug.Log($"ENEMY PIECE AT Row ==  {newRow} Col == {newCol} IS THE PIECE: + {boardRef.board[newRow, newCol]}");

                    if (isCapture) // If enemy piece spotted: Line of sight broken 
                    {
                        //Debug.Log($"THE WHALE MOVE: Row == " + newRow + " Col == " + newCol + " CAPTURES AN ENEMY PIECE. ADDED TO LIST!");
                        moves.Add((newRow, newCol));
                        break;
                    }

                    else if ((color > 0 && BoardArray[newRow, newCol] > 0) || (color < 0 && BoardArray[newRow, newCol] < 0)) // If friendly piece spotted: Line of sight broken AND move is illegal
                    {
                        //Debug.Log($"THE WHALE MOVE: Row == " + newRow + " Col == " + newCol + " CAPTURES FRIENDLY PIECE. REJECTED!");
                        break;
                    }

                    else moves.Add((newRow, newCol));
                    //Debug.Log($"THE WHALE MOVE: Row == " + newRow + " Col == " + newCol + " ADDED TO LIST!");
                }
            }
        }
        return moves;
    }
}