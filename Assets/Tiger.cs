using System.Collections.Generic;

public class Tiger : PieceDATA
{
    private int bounds;

    public override List<(int, int)> GetLegalMoves()
    {
        if (Board.shogiType == "mini")
            bounds = 5;
        else if (Board.shogiType == "chu")
            bounds = 12;
        else bounds = 9;

        if (promoted)
            return StagMove();
        else
            return TigerMove();
    }

    public List<(int, int)> TigerMove()
    {
        List<(int, int)> moves = new List<(int, int)>();

        // Define possible move offsets for a King (relative to its position)
        int[,] moveOffsets = new int[,]
        {
        { -1, -1 }, { -1, 1 }, // Forward moves 
        {  0, -1 }, {  0, 1 },           // Side moves
        { 1, -1 }, { 1, 0 }, { 1, 1 }  // Backward moves
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

    public List<(int, int)> StagMove()
    {
            List<(int, int)> moves = new List<(int, int)>();
            moves.AddRange(ChariotMove());
            moves.AddRange(KingMove());
            return moves;
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
}
