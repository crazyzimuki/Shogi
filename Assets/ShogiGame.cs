using UnityEngine;
using TMPro;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System;
using UnityEngine.UI;
using System.Collections;

public class ShogiGame : MonoBehaviour
{
    int bounds;
    public string shogiType;
    public bool simulating = false;
    public bool gameOver = false;
    public Highlight highPREFAB;  // Prefab for move highlights
    public TMP_Text GameOverText;
    public static ShogiGame Instance { get; private set; }
    public Board board;
    public int color;  // 1 = White (player), -1 = Black (Stockfish)
    public int turn;
    public SimpleStockfishController engine;

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
        if (shogiType == "mini")
            bounds = 5;
        else
            bounds = 9;

        if (bounds == 5)
        HighLightManager.Grid.InitializeGrid(5, 0.425f, Vector3.zero);
        else
            HighLightManager.Grid.InitializeGrid(9, 0.26f, new Vector3(-0.29f, 0.08f, 0f));
        board.PrintBoard(); // Spawn pieces
        ActivatePieces(color);
    }
    #endregion

    #region Turn Management
    public static  void EndTurn()
    {
        if (Instance.board.data.IsCheckMate(Instance.board, -Instance.color))
            EndLife(Instance.color);
        UnityEngine.Debug.Log($"EndTurn: Color={Instance.color}, Turn={Instance.turn}");
        HighLightManager.ClearHighlights();

        if (Instance.color == 0 || Instance.gameOver) return; // Game is over
        if (!Piece.isPromotionUIActive)
        {
            Instance.color *= -1; // Switch player
            Instance.turn++;
        }

        if (Instance.color == 1)
        {
            UnityEngine.Debug.Log("Player's turn (White)");
            ActivatePieces(1);
        }
        else
        {
            UnityEngine.Debug.Log("Stockfish's turn (Black)");
            UnityEngine.Debug.Log("SFEN: " + Instance.board.data.SFEN());
            Instance.engine.RequestEngineMove(Instance.board.data.SFEN(), Instance.handleMoveReceived);
        }
    }
    #endregion

    Action<string> handleMoveReceived = (receivedMove) =>
    {
        if (!string.IsNullOrEmpty(receivedMove))
        {
            // Log that the move was received and apply it
            UnityEngine.Debug.Log($"Stockfish move received: {receivedMove}. Applying...");
            ApplyStockfishMove(receivedMove); // Assuming ApplyStockfishMove handles visuals and switches turn state
        }
        else
        {
            // Log an error if the move is null or empty
            UnityEngine.Debug.LogError("AI failed to provide a valid move!");
            // Decide how to handle this error (e.g., AI forfeits turn, game over?)
            // You might need to manually switch back to the player's turn here if ApplyStockfishMove isn't called.
            // Instance.SwitchToPlayerTurn(); // Example
        }
    };

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
        string from = move.Substring(0, 2);
        string to = move.Substring(2, 2);
        bool promote = move.Length > 4 && move[4] == '+';

        int fromCol = FileToCol(from[0]);
        int fromRow = RankToRow(from[1]);
        int toCol = FileToCol(to[0]);
        int toRow = RankToRow(to[1]);

        // --- Step 1: Identify the correct PIECE DATA ---
        // Use the reliable BoardDATA to find the data of the piece moving.
        // Assumes BoardDATA is correctly updated by CapturePiece!
        PieceDATA pieceDataToMove = Instance.board.data.PieceAt(fromRow, fromCol); // Use BoardDATA.PieceAt

        if (pieceDataToMove == null)
        {
            UnityEngine.Debug.LogError($"ApplyRegularMove: No PieceDATA found at move origin ({fromRow},{fromCol}) for move '{move}'. Check BoardDATA state.");
            return;
        }

        // --- Step 2: Find the corresponding LIVE GameObject/Piece Component ---
        // Need a way to get the Piece component linked to the PieceDATA.
        // Maybe Board has a dictionary or list mapping data to live objects?
        // Example: Piece pieceToMove = Instance.board.FindPieceComponent(pieceDataToMove);
        Piece pieceToMove = pieceDataToMove.pieceRef; // Assuming PieceDATA has a direct reference

        if (pieceToMove == null || pieceToMove.gameObject == null)
        {
            UnityEngine.Debug.LogError($"ApplyRegularMove: Piece component for {pieceDataToMove.pieceType} at ({fromRow},{fromCol}) is null or destroyed! Move '{move}'. Check data/visual sync.");
            return;
        }

        Instance.board.data.MovePiece(pieceDataToMove, toRow, toCol);
        pieceToMove.MovePieceTransform();          // Perform the visual move

        if (promote)
        {
            pieceToMove.Promote();
        }
        else EndTurn();
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
            case 'l': return 9; // Lance
            case 'n': return 8; // Horse
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
        ActivatePieces(0);
        Instance.gameOver = true;
        HighLightManager.ClearHighlights();
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
        return Instance.bounds - fileNum; // File 5 → Col 0, File 1 → Col 4
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
            case 'f': return 5;
            case 'g': return 6;
            case 'h': return 7;
            case 'i': return 8;
            default: throw new Exception($"Invalid rank: {rank}");
        }
    }
    #endregion
}