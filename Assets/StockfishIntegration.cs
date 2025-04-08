using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class StockfishIntegration : MonoBehaviour
{
    private Process process;
    private bool isInitialized = false;

    void Start()
    {
        try
        {
            // Define the path to the Stockfish executable
            string stockfishFileName = "fairy-stockfish-largeboard_x86-64.exe";
            string stockfishPath = Path.Combine(Application.streamingAssetsPath, stockfishFileName);

            // Check if the executable exists
            if (!File.Exists(stockfishPath))
            {
                throw new FileNotFoundException($"Stockfish executable not found at: {stockfishPath}");
            }

            // Initialize the process
            process = new Process();
            process.StartInfo.FileName = stockfishPath;
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(stockfishPath);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();

            // Send initialization commands to Stockfish
            Send("usi");
            ReadUntil("usiok");
            Send("setoption name UCI_Variant value minishogi");
            Send("isready");
            ReadUntil("readyok");

            isInitialized = true;
            UnityEngine.Debug.Log("Stockfish initialized successfully");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to initialize Stockfish engine: {ex.Message}");
            isInitialized = false;
        }
    }

    public string GetStockfishMove(string sfen)
    {
        // Check if the process is ready to use
        if (!isInitialized || process == null || process.HasExited)
        {
            throw new InvalidOperationException("Stockfish process is not initialized or has exited.");
        }

        // Send position and go commands
        Send("position sfen " + sfen);
        Send("go movetime 1000");
        string bestmoveLine = ReadUntil("bestmove");

        // Parse the best move from the response
        string[] parts = bestmoveLine.Split(' ');
        if (parts.Length >= 2)
        {
            return parts[1];
        }
        else
        {
            throw new Exception("Invalid bestmove response from Stockfish");
        }
    }

    private void Send(string command)
    {
        // Ensure the process is valid before sending
        if (process == null || process.HasExited)
        {
            throw new InvalidOperationException("Cannot send command: Stockfish process is not running.");
        }
        process.StandardInput.WriteLine(command);
    }

    private string ReadUntil(string expected)
    {
        // Ensure the process is valid before reading
        if (process == null || process.HasExited)
        {
            throw new InvalidOperationException("Cannot read from Stockfish: process is not running.");
        }
        while (true)
        {
            string line = process.StandardOutput.ReadLine();
            if (line == null)
            {
                throw new Exception("Stockfish output ended unexpectedly");
            }
            if (line.StartsWith(expected))
            {
                return line;
            }
        }
    }

    void OnDestroy()
    {
        // Clean up the process when the GameObject is destroyed
        if (process != null && !process.HasExited)
        {
            process.Kill();
            process.Dispose();
            process = null;
        }
        isInitialized = false;
    }
}