using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Silver : PieceDATA
{
    private int bounds;

    public Silver(bool promoted)
    {
        if (promoted) pieceRef.Promote();
    }

    public override List<(int, int)> GetLegalMoves()
    {
        if (Board.shogiType == "mini")
            bounds = 5;
        else if (Board.shogiType == "chu")
            bounds = 12;
        else bounds = 9;

        if (bounds == 12)
        {
            if (doublepromoted)
                return OxMove();
            else if (promoted)
                return SnakeMove();
            else return SilverGeneralMove();
        }
        else if (!promoted)
            return SilverGeneralMove();
        else return GoldGeneralMove();
    }

    public List<(int, int)> SilverGeneralMove()
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

    public List<(int, int)> SnakeMove()
    {
        List<(int, int)> moves = new List<(int, int)>();

        int newRow = row;
        int newCol = col;

        // Define infinite range move directions for a Snake (relative to its position)
        int[,] moveOffsets = new int[,]
        {
        { 1, 0 },          // Forward move
        { -1, 0 },         // Backward move
        };

        for (int i = 0; i < 2; i++) // For each movement direction
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
                        //Debug.Log($"THE Snake MOVE: Row == " + newRow + " Col == " + newCol + " CAPTURES AN ENEMY PIECE. ADDED TO LIST!");
                        moves.Add((newRow, newCol));
                        break;
                    }

                    else if ((color > 0 && BoardArray[newRow, newCol] > 0) || (color < 0 && BoardArray[newRow, newCol] < 0)) // If friendly piece spotted: Line of sight broken AND move is illegal
                    {
                        //Debug.Log($"THE Snake MOVE: Row == " + newRow + " Col == " + newCol + " CAPTURES FRIENDLY PIECE. REJECTED!");
                        break;
                    }

                    else moves.Add((newRow, newCol));
                    //Debug.Log($"THE Snake MOVE: Row == " + newRow + " Col == " + newCol + " ADDED TO LIST!");
                }
            }
        }

        // Define range 1 move directions for a Snake (relative to its position)
        moveOffsets = new int[,]
        {
        { 0, -1}, { 0, 1}, // Sideways moves
        };

        for (int i = 0; i < 2; i++) // For each movement direction
        {
            // Reset values
            newRow = row;
            newCol = col;

            // Go in direction
            newRow += moveOffsets[i, 0];
            newCol += moveOffsets[i, 1];

            if (newRow >= 0 && newRow < bounds && newCol >= 0 && newCol < bounds) // If still in bounds
            {
                bool isCapture = ((color > 0 && BoardArray[newRow, newCol] < 0) || (color < 0 && BoardArray[newRow, newCol] > 0) && BoardArray[newRow, newCol] != 0);
                //Debug.Log($"ENEMY PIECE AT Row ==  {newRow} Col == {newCol} IS THE PIECE: + {boardRef.board[newRow, newCol]}");

                if (isCapture) // If enemy piece spotted: Line of sight broken 
                {
                    //Debug.Log($"THE Snake MOVE: Row == " + newRow + " Col == " + newCol + " CAPTURES AN ENEMY PIECE. ADDED TO LIST!");
                    moves.Add((newRow, newCol));
                    break;
                }

                else if ((color > 0 && BoardArray[newRow, newCol] > 0) || (color < 0 && BoardArray[newRow, newCol] < 0)) // If friendly piece spotted: Line of sight broken AND move is illegal
                {
                    //Debug.Log($"THE Snake MOVE: Row == " + newRow + " Col == " + newCol + " CAPTURES FRIENDLY PIECE. REJECTED!");
                    break;
                }

                else moves.Add((newRow, newCol));
                //Debug.Log($"THE Snake MOVE: Row == " + newRow + " Col == " + newCol + " ADDED TO LIST!");
            }
        }
        return moves;
    }

    public List<(int, int)> OxMove()
    {
        List<(int, int)> moves = new List<(int, int)>();

        int[,] moveOffsets = new int[,]
{
        { -1, -1 }, { -1, 0 }, { -1, 1 }, // Forward moves 
        { 1, -1 }, { 1, 0 }, { 1, 1 }  // Backward moves
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