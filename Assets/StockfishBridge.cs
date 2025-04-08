using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public class StockfishBridge : MonoBehaviour
{
    private Process stockfishProcess;
    private StreamWriter writer;
    private StreamReader reader;
    public Slider SkillLevel;

    public void StartEngine(string enginePath)
    {
        stockfishProcess = new Process();
        stockfishProcess.StartInfo.FileName = enginePath;
        stockfishProcess.StartInfo.UseShellExecute = false;
        stockfishProcess.StartInfo.RedirectStandardInput = true;
        stockfishProcess.StartInfo.RedirectStandardOutput = true;
        stockfishProcess.StartInfo.CreateNoWindow = true;

        stockfishProcess.Start();

        writer = stockfishProcess.StandardInput;
        reader = stockfishProcess.StandardOutput;

        // 1. Begin USI mode
        SendCommand("usi");
        WaitFor("usiok");

        // 2. Tell the engine you're playing mini shogi
        SendCommand("setoption name USI_Variant value minishogi");

        // 3. (Optional) Set the skill level here if you want
        SendCommand($"setoption name Skill Level value {SkillLevel}");

        // 4. Final handshake
        SendCommand("isready");
        WaitFor("readyok");

        // Now the engine is fully initialized!
    }


    public string GetBestMove(string sfen, int thinkingTimeMs = 1000)
    {
        SendCommand($"position sfen {sfen}");
        SendCommand($"go btime 0 wtime 0 byoyomi {thinkingTimeMs}");

        string bestMove = "";
        while (true)
        {
            string line = reader.ReadLine();
            if (line.StartsWith("bestmove"))
            {
                bestMove = line.Split(' ')[1];
                break;
            }
        }

        return bestMove;
    }

    private void SendCommand(string cmd)
    {
        writer.WriteLine(cmd);
        writer.Flush();
    }

    private void WaitFor(string keyword)
    {
        string line;
        do
        {
            line = reader.ReadLine();
        } while (!line.Contains(keyword));
    }

    public void StopEngine()
    {
        SendCommand("quit");
        stockfishProcess?.Close();
    }
}

