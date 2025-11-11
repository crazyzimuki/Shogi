using System.Collections.Generic;

public class Pawn : PieceDATA
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
            return PawnMove();
        else
            return GoldGeneralMove();
    }

    public List<(int, int)> PawnMove()
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