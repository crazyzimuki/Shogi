using UnityEngine;
using TMPro;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System;

public class ShogiGame : MonoBehaviour
{
    public bool simulating = false;
    public Highlight highPREFAB;  // Prefab for move highlights
    public TMP_Text GameOverText;
    public static ShogiGame Instance { get; private set; }
    public Board board;
    public int color;  // 1 = White (player), -1 = Black (Stockfish)
    public int turn;

    #region Initialization
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initial setup
        turn = 1;
        color = 1; // White starts
        board.PrintBoard(); // Spawn pieces
        ActivatePieces(color);
    }
    #endregion

    #region Turn Management
    public static async void EndTurn()
    {
        UnityEngine.Debug.Log($"EndTurn: Color={Instance.color}, Turn={Instance.turn}");
        HighLightManager.ClearHighlights();

        if (Instance.color == 0) return; // Game is over

        Instance.color *= -1; // Switch player
        Instance.turn++;

        if (Instance.color == 1)
        {
            UnityEngine.Debug.Log("Player's turn (White)");
            ActivatePieces(1);
        }
        else
        {
            UnityEngine.Debug.Log("Stockfish's turn (Black)");
            await StockfishMove();
        }
    }
    #endregion

    #region Stockfish Interaction
    public static async Task StockfishMove()
    {
        string stockfishPath = Path.Combine(Application.dataPath, "fairy-stockfish-largeboard_x86-64.exe");
        if (!File.Exists(stockfishPath))
        {
            UnityEngine.Debug.LogError($"Stockfish not found at: {stockfishPath}");
            return;
        }

        using (var process = new Process())
        {
            process.StartInfo.FileName = stockfishPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            try
            {
                process.Start();
                process.BeginErrorReadLine();
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) UnityEngine.Debug.LogError($"Stockfish error: {e.Data}"); };
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Stockfish start failed: {e.Message}");
                return;
            }

            // Initialize Stockfish
            await process.StandardInput.WriteLineAsync("usi");
            await process.StandardInput.FlushAsync();

            // Wait for initialization confirmation
            if (!await WaitForUsiOk(process))
            {
                UnityEngine.Debug.LogError("Stockfish initialization failed");
                process.Kill();
                return;
            }

            // Configure for Minishogi
            await process.StandardInput.WriteLineAsync("setoption name UCI_Variant value minishogi");
            await process.StandardInput.FlushAsync();

            // Send current position
            string sfen = Instance.board.data.SFEN();
            await process.StandardInput.WriteLineAsync($"position sfen {sfen}");
            await process.StandardInput.FlushAsync();

            // Request move (5ms think time)
            await process.StandardInput.WriteLineAsync("go movetime 5");
            await process.StandardInput.FlushAsync();

            // Get move
            string move = await GetStockfishMove(process);
            if (!string.IsNullOrEmpty(move))
            {
                ApplyStockfishMove(move);
            }

            process.Kill();
        }
    }

    private static async Task<bool> WaitForUsiOk(Process process)
    {
        var timeout = Task.Delay(5000);
        while (!process.HasExited)
        {
            var readTask = process.StandardOutput.ReadLineAsync();
            if (await Task.WhenAny(readTask, timeout) == readTask)
            {
                string line = await readTask;
                if (line?.Contains("usiok") == true) return true;
            }
            else
            {
                return false; // Timeout
            }
        }
        return false;
    }

    private static async Task<string> GetStockfishMove(Process process)
    {
        while (!process.HasExited)
        {
            string line = await process.StandardOutput.ReadLineAsync();
            if (line?.StartsWith("bestmove") == true)
            {
                return line.Split(' ')[1];
            }
        }
        UnityEngine.Debug.LogError("No move received from Stockfish");
        return null;
    }
    #endregion

    #region Move Application
    private static void ApplyStockfishMove(string move)
    {
        UnityEngine.Debug.Log($"Applying Stockfish move: {move}");
        HighLightManager.ClearHighlights();

        if (move.Contains("*"))
        {
            ApplyDropMove(move);
        }
        else
        {
            ApplyRegularMove(move);
        }

        // Check for checkmate against opponent
        if (Instance.board.data.IsCheckMate(Instance.board, -Instance.color))
        {
            EndLife(Instance.color);
        }
    }

    private static void ApplyRegularMove(string move)
    {
        string from = move.Substring(0, 2); // e.g., "3a"
        string to = move.Substring(2, 2);   // e.g., "2b"
        bool promote = move.Length > 4 && move[4] == '+';

        int fromCol = FileToCol(from[0]);
        int fromRow = RankToRow(from[1]);
        int toCol = FileToCol(to[0]);
        int toRow = RankToRow(to[1]);

        Piece piece = Instance.board.PieceAt(fromRow, fromCol);
        if (piece == null)
        {
            UnityEngine.Debug.LogError($"No piece at {from}");
            return;
        }

        Highlight highlight = Instantiate(Instance.highPREFAB);
        highlight.boardRef = Instance.board;
        highlight.parentPiece = piece;
        highlight.move.row = toRow;
        highlight.move.col = toCol;
        highlight.MakeMove();
        // Note: Promotion not implemented in original; add piece.Promote() here if needed
        Destroy(highlight.gameObject);
    }

    private static void ApplyDropMove(string move)
    {
        string[] parts = move.Split('*'); // e.g., "P*3c"
        char pieceChar = parts[0][0];
        string target = parts[1];

        int pieceType = GetPieceType(pieceChar);
        if (pieceType == 0)
        {
            UnityEngine.Debug.LogError($"Invalid piece type: {pieceChar}");
            return;
        }

        int colTarget = FileToCol(target[0]);
        int rowTarget = RankToRow(target[1]);

        DroppedPieceDATA dropToUse = null;
        foreach (DroppedPieceDATA drop in Instance.board.data.droppedPiecesData)
        {
            if (drop.pieceType == pieceType && drop.color == Instance.color)
            {
                dropToUse = drop;
                break;
            }
        }

        if (dropToUse == null)
        {
            UnityEngine.Debug.LogError($"No matching drop piece for {pieceChar}");
            return;
        }

        Highlight highlight = Instantiate(Instance.highPREFAB);
        highlight.boardRef = Instance.board;
        highlight.move.row = rowTarget;
        highlight.move.col = colTarget;
        highlight.MakeAIDropMove(dropToUse);
        Instance.board.data.droppedPiecesData.Remove(dropToUse);
        Destroy(highlight.gameObject);
    }

    private static int GetPieceType(char pieceChar)
    {
        switch (char.ToLower(pieceChar))
        {
            case 'p': return 1; // Pawn
            case 's': return 2; // Silver
            case 'g': return 3; // Gold
            case 'b': return 4; // Bishop
            case 'r': return 5; // Rook
            case 'k': return 7; // King
            default: return 0;
        }
    }
    #endregion

    #region Game State Management
    public static void EndLife(int winningColor)
    {
        string winner = winningColor == 1 ? "White" : "Black";
        UnityEngine.Debug.Log($"Checkmate! {winner} wins");
        Instance.GameOverText.text = $"Checkmate - {winner} Wins!";
        Instance.color = 0; // Halt game
    }

    public static void ActivatePieces(int activeColor)
    {
        foreach (PieceDATA piece in Instance.board.data.Pieces)
        {
            piece.isClickable = (piece.color == activeColor);
        }
    }
    #endregion

    #region Utility Methods
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
    #endregion
}