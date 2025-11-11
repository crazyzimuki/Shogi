using System.Collections.Generic;

public class DoublePawn : PieceDATA
{
    private int bounds;

    public override List<(int, int)> GetLegalMoves()
    {
        if (Board.shogiType == "mini")
            bounds = 5;
        else if (Board.shogiType == "chu")
            bounds = 12;
        else bounds = 9;

        if (doublepromoted)
            return KingMove();
        else if (promoted)
            return ElephantMove();
        else
            return DoublePawnMove();
    }

    public List<(int, int)> DoublePawnMove()
    {
        List<(int, int)> moves = new List<(int, int)>();

        int newRow = row - color;
        //Debug.Log("NEW ROW = " + newRow);

        // Check if the new row is within the board bounds
        if (newRow >= 0 && newRow < bounds)
        {
            // Check if the target square is empty or contains an opponent's piece
            if ((BoardArray[newRow, col] == 0) || ((color > 0 && BoardArray[newRow, col] < 0) || (color < 0 && BoardArray[newRow, col] > 0)))
            {
                moves.Add((newRow, col));
                //Debug.Log("Row == " + newRow + " Col == " + col + " Added to list");
            }
        }

        newRow = row + color;
        //Debug.Log("NEW ROW = " + newRow);

        // Check if the new row is within the board bounds
        if (newRow >= 0 && newRow < bounds)
        {
            // Check if the target square is empty or contains an opponent's piece
            if ((BoardArray[newRow, col] == 0) || ((color > 0 && BoardArray[newRow, col] < 0) || (color < 0 && BoardArray[newRow, col] > 0)))
            {
                moves.Add((newRow, col));
                //Debug.Log("Row == " + newRow + " Col == " + col + " Added to list");
            }
        }
        return moves;
    }

    public List<(int, int)> KingMove()
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

    public List<(int, int)> ElephantMove()
    {
            List<(int, int)> moves = new List<(int, int)>();

            // Define possible move offsets for an Elephant (relative to its position)
            int[,] moveOffsets = new int[,]
            {
            { -1, -1 }, { -1, 0 }, { -1, 1 }, // Forward moves 
            {  0, -1 }, {  0, 1 },           // Side moves
            { 1, -1 }, { 1, 1 }  // Backward moves
            };

            // Try every movement direction
            for (int i = 0; i < 7; i++)
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