using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System;
using System.Threading.Tasks;

public class ShogiGame : MonoBehaviour
{
    public Highlight highPREFAB;  // Prefab for move highlights (used in the real game)
    public TMP_Text GameOverText;
    public static ShogiGame Instance { get; private set; }
    public Board board;
    public int color;         // 1 = White, -1 = Black (White moves first)
    public bool simulating;
    public int turn;
    private void Awake()
    {
        // Singleton initialization.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        turn = 1;
        color = 1;
        board.PrintBoard(); // This spawns pieces
        ActivatePieces(color);
        //UnityEngine.Debug.Log(Instance.board.data.SFEN());
    }

    public static async void EndTurn()
    {
        UnityEngine.Debug.Log($"EndTurn: Current color={Instance.color}, Turn={Instance.turn}");
        HighLightManager.ClearHighlights();
        if (Instance.color != 0)
        {
            Instance.color *= -1;
            Instance.turn++; // Move turn increment here to align with color switch
            if (Instance.color == 1)
            {
                UnityEngine.Debug.Log("Player's turn (White)");
                ActivatePieces(1); // Enable player pieces
            }
            else
            {
                UnityEngine.Debug.Log("Stockfish's turn (Black)");
                await StockfishMove(); // Apply Stockfish move
            }
        }
        UnityEngine.Debug.Log($"Post-EndTurn: Color={Instance.color}, SFEN={Instance.board.data.SFEN()}");
    }

    public static async Task StockfishMove()
    {
        string stockfishPath = Path.Combine(Application.dataPath, "fairy-stockfish-largeboard_x86-64.exe");
        UnityEngine.Debug.Log($"Looking for Stockfish at: {stockfishPath}");

        if (!File.Exists(stockfishPath))
        {
            UnityEngine.Debug.LogError("Stockfish executable not found!");
            return;
        }

        using (var process = new Process())
        {
            process.StartInfo.FileName = stockfishPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            UnityEngine.Debug.Log("Starting Stockfish process...");
            try
            {
                process.Start();
                UnityEngine.Debug.Log("Process started. PID: " + process.Id);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to start Stockfish: {e.Message}");
                return;
            }

            process.ErrorDataReceived += (sender, e) =>
            { if (e.Data != null) UnityEngine.Debug.LogError("Stockfish stderr: " + e.Data); };
            process.BeginErrorReadLine();

            // Initialize Stockfish
            //UnityEngine.Debug.Log("Sending: usi");
            await process.StandardInput.WriteLineAsync("usi");
            await process.StandardInput.FlushAsync();

            // Read until "usiok"
            bool initialized = false;
            string line;
            UnityEngine.Debug.Log("Waiting for Stockfish initialization...");
            Task<string> readTask;
            var timeout = Task.Delay(5000);
            while (!initialized && !process.HasExited)
            {
                readTask = process.StandardOutput.ReadLineAsync();
                if (await Task.WhenAny(readTask, timeout) == readTask)
                {
                    line = await readTask;
                    if (line != null)
                    {
                        //UnityEngine.Debug.Log($"Stockfish says: {line}");
                        if (line.Contains("usiok"))
                        {
                            initialized = true;
                            break;
                        }
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError("Stockfish didn’t respond with 'usiok' within 5 seconds.");
                    break;
                }
            }

            if (!initialized)
            {
                UnityEngine.Debug.LogError("Stockfish failed to initialize (no 'usiok' detected).");
                process.Kill();
                return;
            }
            UnityEngine.Debug.Log("Stockfish initialized successfully!");

            // Set Mini-Shogi variant
            UnityEngine.Debug.Log("Sending: setoption name UCI_Variant value minishogi");
            await process.StandardInput.WriteLineAsync("setoption name UCI_Variant value minishogi");
            await process.StandardInput.FlushAsync();

            // Send position
            string sfen = Instance.board.data.SFEN();
            //UnityEngine.Debug.Log($"Sending: position sfen {sfen}");
            await process.StandardInput.WriteLineAsync($"position sfen {sfen}");
            await process.StandardInput.FlushAsync();

            // Request move with depth limit
            UnityEngine.Debug.Log("Sending: go movetime 5");
            await process.StandardInput.WriteLineAsync("go movetime 5");
            await process.StandardInput.FlushAsync();

            // Read move
            UnityEngine.Debug.Log("Reading Stockfish output...");
            while ((line = await process.StandardOutput.ReadLineAsync()) != null)
            {
                if (line.StartsWith("bestmove"))
                {
                    string move = line.Split(' ')[1];
                    UnityEngine.Debug.Log($"Stockfish chose move: {move}");
                    ApplyStockfishMove(move);
                    break;
                }
            }

            UnityEngine.Debug.Log("Closing Stockfish process...");
            process.Kill();
        }
    }

    private static void ApplyStockfishMove(string move)
    {
        UnityEngine.Debug.Log($"Attempting to apply move: {move}");
        HighLightManager.ClearHighlights();
        Highlight aiHighlight = Instantiate(Instance.highPREFAB);
        aiHighlight.boardRef = Instance.board;

        if (move.Contains("*")) // Drop move, e.g., "P*3c"
        {
            string[] parts = move.Split('*');
            char pieceType = parts[0][0];
            int PIECETYPE = 0;
            switch (pieceType)
            {
                case 'p': case 'P': PIECETYPE = 1; break;
                case 's': case 'S': PIECETYPE = 2; break;
                case 'g': case 'G': PIECETYPE = 3; break;
                case 'b': case 'B': PIECETYPE = 4; break;
                case 'r': case 'R': PIECETYPE = 5; break;
                case 'k': case 'K': PIECETYPE = 7; break;
                default: UnityEngine.Debug.LogError($"Unknown piece type: {pieceType}"); return;
            }
            string target = parts[1]; // e.g., "3c"
            int fileTarget = int.Parse(target[0].ToString()); // '3' -> 3
            int colTarget = 5 - fileTarget; // file 3 -> col 2
            int rowTarget = RankToIndex(target[1]); // 'c' -> 2
            UnityEngine.Debug.Log($"Processing drop: {pieceType} to file {fileTarget}, rank {target[1]} (row={rowTarget}, col={colTarget})");
            bool found = false;
            foreach (DroppedPieceDATA drop in Instance.board.data.droppedPiecesData)
            {
                if (drop.pieceType == PIECETYPE)
                {
                    UnityEngine.Debug.Log($"Found matching drop piece, type {PIECETYPE}, applying at row={rowTarget}, col={colTarget}");
                    aiHighlight.move.row = rowTarget;
                    aiHighlight.move.col = colTarget;
                    aiHighlight.MakeAIDropMove(drop);
                    UnityEngine.Debug.Log($"After drop, checking checkmate for color {-Instance.color}");
                    if (Instance.board.data.IsCheckMate(Instance.board, -Instance.color))
                    {
                        UnityEngine.Debug.Log("Checkmate confirmed");
                        EndLife(Instance.color);
                    }
                    Instance.board.data.droppedPiecesData.Remove(drop); // Remove used drop
                    found = true;
                    break;
                }
            }
            if (!found) UnityEngine.Debug.LogError("No matching dropped piece found to apply the drop move.");
        }
        else // Regular move, e.g., "3a2b"
        {
            string from = move.Substring(0, 2); // e.g., "3a"
            string to = move.Substring(2, 2);   // e.g., "2b"
            bool promote = move.Length > 4 && move[4] == '+';

            int fromCol = FileToCol(from[0]); // '3' → 2
            int fromRow = RankToRow(from[1]); // 'a' → 0
            int toCol = FileToCol(to[0]);     // '2' → 3
            int toRow = RankToRow(to[1]);     // 'b' → 1

            aiHighlight.parentPiece = Instance.board.PieceAt(fromRow, fromCol);
            aiHighlight.move.row = toRow;
            aiHighlight.move.col = toCol;
            UnityEngine.Debug.Log($"Moving piece from {from} to {to}, promote={promote}");
            aiHighlight.MakeMove();
        }

        UnityEngine.Debug.Log($"Stockfish move applied: {move}");
        //UnityEngine.Debug.Log($"New board state: {Instance.board.data.SFEN()}");
        HighLightManager.ClearHighlights();
        Destroy(aiHighlight.gameObject);
        if (Instance.board.data.IsCheckMate(Instance.board, 1)) EndLife(-1);
    }

    private static int RankToIndex(char rank)
    {
        switch (rank)
        {
            case 'a': return 0;
            case 'b': return 1;
            case 'c': return 2;
            case 'd': return 3;
            case 'e': return 4;
            default: throw new Exception($"Invalid rank: {rank}");
        }
    }
/* public static void ComputerMove()
{
HighLightManager.ClearHighlights();
Highlight aiHighlight = Instantiate(Instance.highPREFAB);
Instance.simulating = true;
var AIMove = AlphaBeta(Instance.board, int.MinValue, int.MaxValue, 2, -1);
Instance.simulating = false;
if (AIMove.move.Equals((-1, -1)) || AIMove.piece == null)
    {
        UnityEngine.Debug.LogWarning("AI found no legal move.");
        return;
    }

    aiHighlight.move = AIMove.move;
    aiHighlight.boardRef = Instance.board;
    aiHighlight.parentPiece = AIMove.piece.pieceRef;
    UnityEngine.Debug.Log($"AI MOVE: {aiHighlight.move.row}, {aiHighlight.move.col}");
    UnityEngine.Debug.Log($"MOVE SCORE: {AIMove.score}");

    // Make move or drop
    if (AIMove.isDrop == false)
        aiHighlight.MakeMove();
    else
    {
        UnityEngine.Debug.Log("MAKING DROP MOVE");
        foreach (DroppedPieceDATA dp in Instance.board.data.droppedPiecesData)
        {
            UnityEngine.Debug.Log($"DP.DATA.PIECETYPE = {dp.pieceType}, AIMOVE.PIECE.PIECETYPE = {AIMove.piece.pieceType}");
            if (dp.pieceType == AIMove.piece.pieceType)
            {
                UnityEngine.Debug.Log("Drop Found!");
                UnityEngine.Debug.Log($"Dropping {dp.pieceType * dp.color} at {AIMove.move.row}, {AIMove.move.col}");
                aiHighlight.MakeAIDropMove(dp);
                Instance.board.data.droppedPiecesData.Remove(dp);
                break;
            }
        }
    }
    Destroy(aiHighlight.gameObject);
}*/

public static void EndLife(int winningColor)
    {
        UnityEngine.Debug.Log($"Checkmate! Winner is {(winningColor == 1 ? "White" : "Black")}");
        Instance.GameOverText.text = (winningColor == 1) ? "Checkmate - White Wins!" : "Checkmate - Black Wins!";
        Instance.color = 0; // Stop further moves.
    }

    public int TerminalState(int color)
    {
        return (color == 1) ? int.MaxValue : int.MinValue;
    }

    public static void ActivatePieces(int activeColor)
    {
        foreach (PieceDATA p in Instance.board.data.Pieces)
            p.isClickable = (p.color == activeColor);
    }

    /*public static (int score, (int row, int col) move, PieceDATA piece, bool isDrop) AlphaBeta(Board board, int alpha, int beta, int depth, int color)
    {
        string indent = new string(' ', (5 - depth) * 4);

        // Base case: evaluate the board at depth 0
        if (depth == 0)
        {
            int eval = NodeEvaluation(board);
            return (eval, (-1, -1), null, false);
        }

        // Initialize best move and score
        int bestScore = (color == 1) ? int.MinValue : int.MaxValue;
        (int row, int col) bestMove = (-1, -1);
        PieceDATA bestPiece = null;
        bool moveFound = false;
        bool isDrop = false;

        // Get all pieces and dropped pieces for the current player
        List<PieceDATA> pieces = board.data.AllPiecesOfColor(color, board.data.Pieces);
        List<DroppedPieceDATA> relevantDrops = board.data.droppedPiecesData.FindAll(d => d.color == color);

        // Evaluate regular moves
        foreach (PieceDATA p in pieces)
        {
            if (p == null)
                continue;

            List<(int row, int col)> moves = p.GetLegalMoves();

            if (moves != null)
                moveFound = true;

            foreach (var move in moves)
            {
                BoardDATA simData = board.data.Clone();
                Board simBoard = new Board { data = simData };

                int target = simData.board[move.row, move.col];
                if (target != 0 && ((color == 1 && target > 0) || (color == -1 && target < 0)))
                    continue;

                PieceDATA pCopy = simData.PieceAt(simData, p.row, p.col);
                if (pCopy == null)
                    continue;

                simData.MovePiece(simData, pCopy, move.row, move.col);

                if (simData.isCheck(color, simData.Pieces))
                    continue;

                var (score, _, _, _) = AlphaBeta(simBoard, alpha, beta, depth - 1, -color);

                if (color == 1)
                {
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                        bestPiece = p;
                        isDrop = false;
                    }
                    alpha = Mathf.Max(alpha, bestScore);
                }
                else
                {
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                        bestPiece = p;
                        isDrop = false;
                    }
                    beta = Mathf.Min(beta, bestScore);
                }

                if (alpha >= beta)
                    break;
            }
        }

        // Evaluate drops
        foreach (DroppedPieceDATA p in relevantDrops)
        {
            List<(int row, int col)> moves = p.GenerateMoves();
            if (moves != null)
                moveFound = true;

            foreach (var move in moves)
            {
                BoardDATA simData = board.data.Clone();
                Board simBoard = new Board { data = simData };

                int target = simData.board[move.row, move.col];
                if (target != 0 && ((color == 1 && target > 0) || (color == -1 && target < 0)))
                    continue;

                p.MakeSimulatedDrop(move, simData);

                if (simData.isCheck(color, simData.Pieces))
                    continue;

                var (score, _, _, _) = AlphaBeta(simBoard, alpha, beta, depth - 1, -color);

                if (color == 1)
                {
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                        bestPiece = board.CreatePieceDataByType(p.pieceType);
                        bestPiece.color = color;
                        bestPiece.pieceType = p.pieceType;
                        isDrop = true;
                    }
                    alpha = Mathf.Max(alpha, bestScore);
                }
                else
                {
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                        bestPiece = board.CreatePieceDataByType(p.pieceType);
                        bestPiece.color = color;
                        bestPiece.pieceType = p.pieceType;
                        isDrop = true;
                    }
                    beta = Mathf.Min(beta, bestScore);
                }

                if (alpha >= beta)
                    break;
            }
        }

        if (!moveFound)
            return (Instance.TerminalState(color), (-1, -1), null, false);

        return (bestScore, bestMove, bestPiece, isDrop);
    }

    public static int NodeEvaluation(Board board)
    {
        int whiteEval = 0, blackEval = 0;
        var whitePieces = board.data.AllPiecesOfColor(1, board.data.Pieces);
        var blackPieces = board.data.AllPiecesOfColor(-1, board.data.Pieces);

        // Calculate material score for non-terminal states
        foreach (var p in whitePieces.Where(p => !p.simulatedCapture))
            whiteEval += MaterialScore(p);
        foreach (var p in blackPieces.Where(p => !p.simulatedCapture))
            blackEval += MaterialScore(p);

        int eval = whiteEval - blackEval;
        return eval;
    }*/

    public static int MaterialScore(PieceDATA p)
    {
        if (p.pieceType == 5) return (p.promoted ? 12 : 10);
        if (p.pieceType == 4) return (p.promoted ? 10 : 8);
        if (p.pieceType == 3) return 6;
        if (p.pieceType == 2) return 5;
        if (p.pieceType == 1) return 1;
        return 0;
    }

    private static int FileToCol(char file)
    {
        int fileNum = int.Parse(file.ToString());
        return 5 - fileNum; // File 5 → Col 0, File 1 → Col 4
    }

    private static int RankToRow(char rank)
    {
        switch (rank)
        {
            case 'a': return 0;
            case 'b': return 1;
            case 'c': return 2;
            case 'd': return 3;
            case 'e': return 4;
            default: throw new Exception($"Invalid rank: {rank}");
        }
    }
}