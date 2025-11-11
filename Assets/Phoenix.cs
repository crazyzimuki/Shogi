using System.Collections.Generic;

public class Phoenix : PieceDATA
{
    private int bounds;

    public Phoenix(bool promoted)
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

        if (!promoted)
            return PhoenixMove();
        else
            return QueenMove();
    }

    public List<(int, int)> PhoenixMove()
    {
        List<(int, int)> moves = new List<(int, int)>();

        // Define possible move offsets for a Kirin (relative to its position)
        int[,] moveOffsets = new int[,]
        {
        { -2, -1 }, { -1, 0 }, { -2, 1 }, // Forward moves 
        {  0, -1 }, {  0, 1 },           // Side moves
        { 2, -2 }, {1, 0 }, { 2, 2 }  // Backward moves
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

    public List<(int, int)> QueenMove()
    {
        List<(int, int)> moves = new List<(int, int)>();
        int newRow = row;
        int newCol = col;

        // Define possible move offsets for a King (relative to its position)
        int[,] moveOffsets = new int[,]
        {
        { -1, -1 }, { -1, 0 }, { -1, 1 }, // Forward moves 
        {  0, -1 }, {  0, 1 },           // Side moves
        { 1, -1 }, { 1, 0 }, { 1, 1 }  // Backward moves
        };

        for (int i = 0; i < 8; i++) // For each movement direction
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
                        //Debug.Log($"THE ROOK MOVE: Row == " + newRow + " Col == " + newCol + " CAPTURES AN ENEMY PIECE. ADDED TO LIST!");
                        moves.Add((newRow, newCol));
                        break;
                    }

                    else if ((color > 0 && BoardArray[newRow, newCol] > 0) || (color < 0 && BoardArray[newRow, newCol] < 0)) // If friendly piece spotted: Line of sight broken AND move is illegal
                    {
                        //Debug.Log($"THE ROOK MOVE: Row == " + newRow + " Col == " + newCol + " CAPTURES FRIENDLY PIECE. REJECTED!");
                        break;
                    }

                    else moves.Add((newRow, newCol));
                    //Debug.Log($"THE ROOK MOVE: Row == " + newRow + " Col == " + newCol + " ADDED TO LIST!");
                }
            }
        }
        return moves;
    }
}
