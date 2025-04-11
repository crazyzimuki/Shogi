using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class BoardDATA
{
    public Board boardRef;
    public int[,] board;
    public int[,] initialBoardSetup = new int[5, 5] {
        { -5, -4, -2, -3, -7 },
        {  0,  0,  0,  0, -1 },
        {  0,  0,  0,  0,  0 },
        {  1,  0,  0,  0,  0 },
        {  7,  3,  2,  4,  5 }
    };

    public List<PieceDATA> Pieces = new List<PieceDATA>();
    public List<DroppedPieceDATA> droppedPiecesData = new List<DroppedPieceDATA>();
    public BoardDATA()
    {
        board = new int[5, 5];
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                board[i, j] = initialBoardSetup[i, j];
    }

    public void InitializeBoard()
    {
        boardRef = GameObject.Find("Board").GetComponent<Board>();
        for (int r = 0; r < 5; r++)
            for (int c = 0; c < 5; c++)
                board[r, c] = initialBoardSetup[r, c];
    }

    public PieceDATA PieceAt(int row, int col)
    {
        return Pieces.FirstOrDefault(p => p.row == row && p.col == col);
    }

    public void ModifyBoard(int row, int col, int pieceType, int color)
    {
        int target = board[row, col];
        if (pieceType != 0 && target != 0)
        {
            int targetColor = (target > 0) ? 1 : -1;
            if (targetColor != color)
            {
                if (ShogiGame.Instance.simulating)
                    SimulatedCapture(row, col);
                else
                CapturePiece(target, row, col, color);
            }
            else
                return;
        }
        board[row, col] = pieceType * color;
    }

    public void MovePiece(PieceDATA p, int row, int col)
    {
        int oldRow = p.row, oldCol = p.col;
        ModifyBoard(oldRow, oldCol, 0, 0);
        ModifyBoard(row, col, p.pieceType, p.color);
        p.row = row;
        p.col = col;
        //Debug.Log($"MovePiece: Placed {p.pieceType * p.color} at ({row},{col})");
    }

    public void CapturePiece(int pieceType, int row, int col, int color)
    {
        Debug.Log($"Real capture: Piece Type {pieceType}, Color {-color} at ({row},{col})");
        PieceDATA targetPiece = PieceAt(row, col); 
        if (targetPiece == null)
        {
            Debug.LogError($"CapturePiece: No PieceDATA found at ({row},{col}) to capture.");
            return;
        }

        targetPiece.promoted = false; // Safe

        // Check pieceRef BEFORE using it
        if (targetPiece.pieceRef == null)
        {
            //Debug.LogError($"CapturePiece: targetPiece {targetPiece.pieceType} at ({row},{col}) has null pieceRef!");
            Pieces.Remove(targetPiece); // Remove from data list anyway
            return;
        }

        boardRef.CreateNewDrop(targetPiece.pieceRef); 
        Pieces.Remove(targetPiece);
        Board.DestroyPiece(targetPiece.pieceRef.gameObject); 
    }

    public void SimulatedCapture(int row, int col)
    {
        PieceDATA targetPiece = PieceAt(row, col);
        if (targetPiece != null)
        {
            targetPiece.simulatedCapture = true;
            Pieces.Remove(targetPiece);
            // Add to opponent's dropped pieces
            DroppedPieceDATA newDrop = new DroppedPieceDATA { pieceType = targetPiece.pieceType, color = -targetPiece.color, promoted = false };
            droppedPiecesData.Add(newDrop);
        }
        board[row, col] = 0;
    }

    public List<(int, int)> AllLegalMoves(int color)
    {
        List<(int, int)> allMoves = new List<(int, int)>();
        foreach (PieceDATA p in Pieces)
        {
            if (p.color == color)
                allMoves.AddRange(p.GetLegalMoves() ?? new List<(int, int)>());
        }
        return allMoves;
    }

    public List<PieceDATA> AllPiecesOfColor(int color, List<PieceDATA> pieces)
    {
        return pieces.FindAll(p => p.color == color);
    }

    public List<DroppedPieceDATA> AllDroppedPiecesDataOfColor(int color)
    {
        return droppedPiecesData.FindAll(d => d.color == color);
    }

    public void PrintBoardToConsole()
    {
        Debug.Log("--- Board State ---");
        for (int i = 0; i < 5; i++)
        {
            string line = "";
            for (int j = 0; j < 5; j++)
                line += board[i, j].ToString().PadLeft(3) + ", ";
            Debug.Log(line);
        }
        Debug.Log("--- End Board State ---");
    }

    public BoardDATA Clone()
    {
        BoardDATA copy = new BoardDATA(false);
        copy.board = (int[,])this.board.Clone();
        copy.Pieces = new List<PieceDATA>();
        foreach (PieceDATA p in this.Pieces)
        {
            PieceDATA pCopy = p.Copy(null);
            pCopy.simulationBoardData = copy; // Ensure simulation board link
            copy.Pieces.Add(pCopy);
        }
        copy.droppedPiecesData = new List<DroppedPieceDATA>();
        foreach (DroppedPieceDATA dp in droppedPiecesData)
        {
            DroppedPieceDATA dpCopy = new DroppedPieceDATA { pieceType = dp.pieceType, color = dp.color, promoted = dp.promoted };
            dpCopy.boardRef = null; // No live board in simulation
            copy.droppedPiecesData.Add(dpCopy);
        }
        return copy;
    }

    public BoardDATA(bool initialize)
    {
        board = new int[5, 5];
        if (initialize)
        {
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                    board[i, j] = initialBoardSetup[i, j];
        }
    }

    public bool IsCheckMate(Board B, int color)
    {
        //Debug.Log($"Checking checkmate for color {color}, Pieces count: {Pieces.Count}");
        //Debug.Log($"Piece: {p.pieceType} at ({p.row},{p.col}), color {p.color}");
        if (!isCheck(color, Pieces))
            return false;

        List<PieceDATA> relevant = AllPiecesOfColor(color, Pieces);
        List<DroppedPieceDATA> relevantDrops = AllDroppedPiecesDataOfColor(-color);
        Piece newPiss = null;
        foreach (PieceDATA p in relevant)
        {
            List<(int r, int c)> moves = p.GetLegalMoves();
            foreach (var move in moves)
            {
                BoardDATA simCopy = Clone();
                PieceDATA pCopy = simCopy.PieceAt(p.row, p.col);
                if (pCopy == null) continue;
                simCopy.MovePiece(pCopy, move.r, move.c);
                if (!isCheck(color, simCopy.Pieces))
                {
                    Debug.Log($"THE MOVE {p.pieceType * p.color} TO ({move.Item1}, {move.Item2}) ESCAPES CHECK!");
                    return false;
                }
            }
        }

        // Look at all of the player's drop moves
        foreach (DroppedPieceDATA dp in relevantDrops)
        {
            List<(int r, int c)> moves = dp.GenerateMoves();
            foreach ((int row, int col) move in moves)
            {
                // Copy the board and pieces
                Board simCopy = boardRef.CopyBoard();
                // Simulate the drop
                newPiss = SimulateAIDropMove(simCopy.data, dp, move.row, move.col);
                // Are we still in check after the drop?
                if (!isCheck(color, simCopy.data.Pieces))
                {
                    GameObject.Destroy(newPiss);
                    return false;
                }
                if (newPiss != null)
                    GameObject.Destroy(newPiss);
            }
        }            
        Debug.Log("No escape found - Checkmate");
        return true;
    }

    public Piece SimulateAIDropMove(BoardDATA bb, DroppedPieceDATA drop, int row, int col)
    {
        bb.ModifyBoard(row, col, drop.pieceType, drop.color);
        Piece newPiss = bb.boardRef.SpawnPieceType(drop.pieceType, row, col);
        /*PieceDATA newPiece = bb.boardRef.CreatePieceDataByType(drop.pieceType);
        newPiece.row = row; newPiece.col = col; newPiece.color = drop.color; newPiece.boardRef = bb.boardRef; newPiece.pieceType = drop.pieceType;
        Pieces.Add(newPiece);
        return newPiece;*/
        return newPiss;
    }

    public bool isCheck(int color, List<PieceDATA> pieces)
    {
        var (kingRow, kingCol) = FindKing(color, pieces);
        List<PieceDATA> opponent = AllPiecesOfColor(-color, pieces);
        foreach (PieceDATA p in opponent)
        {
            var moves = p.GetLegalMoves();
            //Debug.Log($"Opponent {p.pieceType} at ({p.row},{p.col}) moves: {string.Join(", ", moves)}");
            if (moves.Any(m => m.Item1 == kingRow && m.Item2 == kingCol)) return true;
        }
        return false;
    }

    public (int kingRow, int kingCol) FindKing(int color, List<PieceDATA> pieces)
    {
        foreach (PieceDATA p in pieces)
        {
            if (p.pieceType == 7 && p.color == color)
                return (p.row, p.col);
        }
        return (-9, -9);
    }

    // Converts the current boardstate to an SFEN string
    public string SFEN()
    {
        string s = string.Empty;

        // For each rank on the board
        for (int i = 0; i < 5; i++)
        {
            if (i != 0) s += '/';
            int emptyCounter = 0; // Count empty squares in a row
            bool lastEmpty = false; // Was last square empty?

            // For each file in the rank
            for (int j = 0; j < 5; j++)
            {
                // Convert numbers to SFEN letters
                switch (board[i, j])
                {
                    case -7: if (lastEmpty) { s += emptyCounter; emptyCounter = 0; lastEmpty = false; } s += 'k'; break;
                    case -5: if (lastEmpty) { s += emptyCounter; emptyCounter = 0; lastEmpty = false; } if (PieceAt(i, j).promoted) s += '+'; s += 'r'; break;
                    case -4: if (lastEmpty) { s += emptyCounter; emptyCounter = 0; lastEmpty = false; } if (PieceAt(i, j).promoted) s += '+'; s += 'b'; break;
                    case -3: if (lastEmpty) { s += emptyCounter; emptyCounter = 0; lastEmpty = false; } s += 'g'; break;
                    case -2: if (lastEmpty) { s += emptyCounter; emptyCounter = 0; lastEmpty = false; } if (PieceAt(i, j).promoted) s += '+'; s += 's'; break;
                    case -1: if (lastEmpty) { s += emptyCounter; emptyCounter = 0; lastEmpty = false; } if (PieceAt(i, j).promoted) s += '+'; s += 'p'; break;
                    case 7: if (lastEmpty) { s += emptyCounter; emptyCounter = 0; lastEmpty = false; } s += 'K'; break;
                    case 5: if (lastEmpty) { s += emptyCounter; emptyCounter = 0; lastEmpty = false; } if (PieceAt(i, j).promoted) s += '+'; s += 'R'; break;
                    case 4: if (lastEmpty) { s += emptyCounter; emptyCounter = 0; lastEmpty = false; } if (PieceAt(i, j).promoted) s += '+'; s += 'B'; break;
                    case 3: if (lastEmpty) { s += emptyCounter; emptyCounter = 0; lastEmpty = false; } s += 'G'; break;
                    case 2: if (lastEmpty) { s += emptyCounter; emptyCounter = 0; lastEmpty = false; } if (PieceAt(i, j).promoted) s += '+'; s += 'S'; break;
                    case 1: if (lastEmpty) { s += emptyCounter; emptyCounter = 0; lastEmpty = false; } if (PieceAt(i, j).promoted) s += '+'; s += 'P';  break;
                    case 0: emptyCounter++; lastEmpty = true; break;
                }
            }
            if (emptyCounter > 0) s += emptyCounter;
        }

        s += ' ';
        if (ShogiGame.Instance.turn%2 == 0) s += 'w';
        else s += 'b';
        s += ' ';

        // count captured pieces
        int whiteKingCounter = 0;
        int blackKingCounter = 0;
        int whiteRookCounter = 0;
        int blackRookCounter = 0;
        int whiteBishopCounter = 0;
        int blackBishopCounter = 0;
        int whiteGoldCounter = 0;
        int blackGoldCounter = 0;
        int whiteSilverCounter = 0;
        int blackSilverCounter = 0;
        int whitePawnCounter = 0;
        int blackPawnCounter = 0;

        // Check if there are any drops
        if (droppedPiecesData.Count == 0)
            s += '-';
        else
        // Count how many white captured pieces of each type
        foreach (DroppedPieceDATA drop in droppedPiecesData)
        {
            if (drop.color == -1) continue; // Skip black pieces
            switch (drop.pieceType)
            {
                case 7: whiteKingCounter++; if (whiteKingCounter > 1) s += whiteKingCounter; s += 'K'; break;
                case 5: whiteRookCounter++; if (whiteRookCounter > 1) s += whiteRookCounter; s += 'R'; break;
                case 4: whiteBishopCounter++; if (whiteBishopCounter > 1) s += whiteBishopCounter; s += 'B'; break;
                case 3: whiteGoldCounter++; if (whiteGoldCounter > 1) s += whiteGoldCounter; s += 'G'; break;
                case 2: whiteSilverCounter++; if (whiteSilverCounter > 1) s += whiteSilverCounter; s += 'S'; break;
                case 1: whitePawnCounter++; if (whitePawnCounter > 1) s += whitePawnCounter; s += 'P'; break;
            }
        }

        // Count how many black captured pieces of each type
        foreach (DroppedPieceDATA drop in droppedPiecesData)
        {
            if (drop.color == 1) continue; // Skip white pieces
            switch (drop.pieceType)
            {
                case 7: blackKingCounter++; if (blackKingCounter > 1) s += blackKingCounter; s += 'k'; break;
                case 5: blackRookCounter++; if (blackRookCounter > 1) s += blackRookCounter; s += 'r'; break;
                case 4: blackBishopCounter++; if (blackBishopCounter > 1) s += blackBishopCounter; s += 'b'; break;
                case 3: blackGoldCounter++; if (blackGoldCounter > 1) s += blackGoldCounter; s += 'g'; break;
                case 2: blackSilverCounter++; if (blackSilverCounter > 1) s += blackSilverCounter; s += 's'; break;
                case 1: blackPawnCounter++; if (blackPawnCounter > 1) s += blackPawnCounter; s += 'p'; break;
            }
        }

        s += ' ';
        s += ShogiGame.Instance.turn;

        // END
        return s;
    }

    public AIMove USI_to_Move(string USI)
    {
        int fromCol = 0, fromRow = 0, toCol = 0, toRow = 0;
        bool promote = false;
        if (USI.Contains('+')) promote = true;

        // Drop move
        if (USI.Contains('*')) // Example: S*4c or B*5b
        {
            fromRow = -1; // Signifying the move is a drop
            toCol = (int)Char.GetNumericValue(USI[2])-1;
            switch (USI[3])
            {
                case 'e': toRow = 4; break;
                case 'd': toRow = 3; break;
                case 'c': toRow = 2; break;
                case 'b': toRow = 1; break;
                case 'a': toRow = 0; break;
            }
            fromCol = LetterToNumber(USI[0]); // pieceType to drop
            return new AIMove(new Vector4(fromRow, fromCol, toRow, toCol), false);
        }
        // Regular move
        else
        {
            fromCol = 5- (int)Char.GetNumericValue(USI[0]);
            switch (USI[1])
            {
                case 'e': fromRow = 4; break;
                case 'd': fromRow = 3; break;
                case 'c': fromRow = 2; break;
                case 'b': fromRow = 1; break;
                case 'a': fromRow = 0; break;
            }
            toCol = 5- (int)Char.GetNumericValue(USI[2]);
            switch (USI[3])
            {
                case 'e': toRow = 4; break;
                case 'd': toRow = 3; break;
                case 'c': toRow = 2; break;
                case 'b': toRow = 1; break;
                case 'a': toRow = 0; break;
            }
        }
        return new AIMove (new Vector4(fromRow, fromCol, toRow, toCol), promote);
    }

    public int LetterToNumber (char c)
    {
        switch (c)
        {
            case 'p': case 'P' : return 1;
            case 's': case 'S': return 2; 
            case 'g': case 'G': return 3;
            case 'b': case 'B': return 4;
            case 'r': case 'R': return 5;
        }
        return 0;
    }

    // Makes the AI's move, updates board state and pieces
    public void MakeAIMove (AIMove move)
    {
        Highlight AImove = new Highlight();
        AImove.boardRef = boardRef;
        AImove.move.row = (int)move.move.z;
        AImove.move.col = (int)move.move.w;

        // Drop
        if (move.move.x == -1) // If move.FromRow == -1
        {
            foreach (DroppedPieceDATA drop in AllDroppedPiecesDataOfColor(-1))
            {
                if (drop.pieceType == move.move.y) // FromCol has the pieceType
                {
                    Debug.Log("Dropped piece found!");
                    AImove.MakeAIDropMove(drop);
                }
            }
        }
        else // Regular move
        {
            AImove.parentPiece = boardRef.PieceAt((int)move.move.x, (int)move.move.y);
            Debug.Log("AImove parentPiece is " + AImove.parentPiece);
            AImove.MakeMove();
        }
    }

    public class AIMove
    {
        public Vector4 move;
        /// <summary>
        /// IMPORTANT!!!
        /// move.x = FromRow
        /// move.y = FromCol
        /// move.z = ToRow
        /// move.w = ToCol
        /// KEEP THIS CONSISTENT ACROSS PROGRAM
        /// </summary>
        public bool promote;

        public AIMove(Vector4 move, bool promote = false)
        {
            this.move = move;
            this.promote = promote;
        }

        public void Print()
        {
            Debug.Log("FromRow = " + move.x);
            Debug.Log("FromCol = " + move.y);
            Debug.Log("ToRow = " + move.z);
            Debug.Log("ToCol = " + move.w);
            Debug.Log("promote = " + promote);
        }
    }
}