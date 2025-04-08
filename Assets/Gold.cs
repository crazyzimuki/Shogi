
using System.Collections.Generic;

public class Gold : PieceDATA
{
    public Gold()
    {
        if (color < 0) pieceRef.FlipVertically();
        return; // No UI bullshit since can't promote
    }

    public override void CheckPromotion()
    {
        return; // Gold can't promote
    }

    public override List<(int, int)> GetLegalMoves()
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
            int newRow = row + moveOffsets[i,0];
            int newCol = col + moveOffsets[i,1];

            // Check if the new position is within the board bounds
            if (newRow >= 0 && newRow < 5 && newCol >= 0 && newCol < 5)
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