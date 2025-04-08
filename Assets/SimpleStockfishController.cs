using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TMPro;

// Remember to set engineExecutableName in the Inspector!
// Example: fairy-stockfish-largeboard_x86-64-modern.exe (or your specific build)

public class SimpleStockfishController : MonoBehaviour
{
    public bool IsEngineReadyForGame => isEngineInitialized && isEngineConfigured;

    [Header("Engine Settings")]
    [Tooltip("The EXACT filename of the Fairy Stockfish executable within StreamingAssets")]
    public string engineExecutableName = "fairy-stockfish-largeboard_x86-64-bmi2.exe";

    [Header("UI Elements")]
    public Slider eloSlider;
    public TMP_Text difficultyText; // Text to display the selected Elo
    public Button startButton; // Start game

    // --- Engine Communication ---
    private Process engineProcess;
    private StreamWriter engineWriter;
    private StreamReader engineReader;
    private string enginePath;

    // --- State ---
    private bool isEngineInitialized = false;
    private bool isEngineConfigured = false; // New state to track if difficulty is set
    private bool needsUciNewGame = false;

    #region Unity Lifecycle & UI Setup

    void Awake()
    {
        // --- Validate UI Element Connections ---
        bool uiValid = true;
        if (eloSlider == null) { UnityEngine.Debug.LogError("Elo Slider is not assigned!"); uiValid = false; }
        if (startButton == null) { UnityEngine.Debug.LogError("Start Button is not assigned!"); uiValid = false; }

        if (!uiValid)
        {
            UnityEngine.Debug.LogError("UI elements missing, disabling Stockfish Controller.");
            this.enabled = false;
            return;
        }

        // --- Setup UI Listeners ---
        eloSlider.onValueChanged.AddListener(OnEloChanged);
        startButton.onClick.AddListener(OnStartGameButtonPressed);

        // --- Initial UI State ---
        if (difficultyText != null)
        {
            difficultyText.text = $"{Mathf.RoundToInt(eloSlider.value)+1}"; // Set initial text
        }
    }

    void Start()
    {
        enginePath = Path.Combine(Application.streamingAssetsPath, engineExecutableName);

        // --- Validate Engine Path ---
        if (string.IsNullOrEmpty(engineExecutableName))
        {
            UnityEngine.Debug.LogError("Engine executable name is empty in the Inspector!");
            this.enabled = false;
            return;
        }

        // Check file existence *before* trying to start
        if (!File.Exists(enginePath))
        {
            UnityEngine.Debug.LogError($"Engine executable not found at path: {enginePath}\nEnsure '{engineExecutableName}' is correct and exists in Assets/StreamingAssets/");
            this.enabled = false;
            return;
        }

        // --- Start Engine Initialization ---
        UnityEngine.Debug.Log($"Engine path seems valid, attempting initialization: {enginePath}");
        StartCoroutine(InitializeEngineCoroutine());
    }

    void OnDestroy()
    {
        CleanupEngineResources();
    }

    void OnApplicationQuit()
    {
        CleanupEngineResources();
    }

    #endregion

    #region UI Callbacks

    void OnDifficultyChanged(int value)
    {
        // Option 3 is usually "Custom Elo" if it's the 4th item (index 3)
        eloSlider.gameObject.SetActive(value == 3);
    }

    void OnEloChanged(float value)
    {
        if (difficultyText != null)
        {
            difficultyText.text = $"{Mathf.RoundToInt(value)+1}";
        }
    }

    // Called when the "Start Game" button is pressed
    void OnStartGameButtonPressed()
    {
        // Determine settings from UI
        int skillLevel = 0;
        StartCoroutine(ConfigureEngineDifficulty(skillLevel));

        // Disable button after starting configuration to prevent spamming
        startButton.interactable = false;
    }

    #endregion

    #region Engine Initialization and Configuration

    IEnumerator InitializeEngineCoroutine()
    {
        UnityEngine.Debug.Log("Starting Engine Initialization...");
        isEngineInitialized = false;
        isEngineConfigured = false;
        startButton.interactable = false; // Ensure button is disabled during init

        bool didStart = false;
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = enginePath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true, // Capture errors too
                CreateNoWindow = true
            };

            engineProcess = new Process { StartInfo = startInfo };

            // Setup error handling *before* starting
            engineProcess.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data)) UnityEngine.Debug.LogError($"Engine ERR: {args.Data}");
            };

            didStart = engineProcess.Start();

            if (didStart)
            {
                engineProcess.BeginErrorReadLine(); // Start reading error stream async
                engineWriter = engineProcess.StandardInput;
                engineReader = engineProcess.StandardOutput;
                UnityEngine.Debug.Log("Engine process started successfully.");
            }
            else
            {
                UnityEngine.Debug.LogError("engineProcess.Start() returned false. Check permissions and path.");
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to start engine process ({enginePath}): {e.Message}\n{e.StackTrace}");
            engineProcess?.Dispose(); // Dispose if partially created
            engineProcess = null;
            didStart = false;
        }

        if (!didStart)
        {
            UnityEngine.Debug.LogError("Engine process did not start. Initialization aborted.");
            CleanupEngineResources();
            // Maybe disable UI or show an error message permanently
            yield break; // Stop the coroutine
        }

        // --- USI Handshake (Correct for Shogi) ---
        bool usiOkReceived = false;
        // Send "usi", expect "usiok"
        yield return SendCommandAndWaitForResponse("usi", "usiok", 5.0f, (success) => usiOkReceived = success); // Increased timeout slightly
        if (!usiOkReceived)
        {
            UnityEngine.Debug.LogError("Engine did not respond with 'usiok'. USI handshake failed. Ensure engine supports USI (Shogi).");
            CleanupEngineResources();
            yield break;
        }
        UnityEngine.Debug.Log("USI handshake successful ('usiok' received).");


        // --- Set Shogi Variant ---
        SendCommand("setoption name UCI_Variant value minishogi");

        // --- Check Readiness ---
        bool readyOkReceived = false;
        yield return SendCommandAndWaitForResponse("isready", "readyok", 3.0f, (success) => readyOkReceived = success);
        if (!readyOkReceived)
        {
            UnityEngine.Debug.LogError("Engine did not respond with 'readyok'. Readiness check failed.");
            CleanupEngineResources();
            yield break;
        }

        // --- Initialization Complete ---
        isEngineInitialized = true;
        UnityEngine.Debug.Log("Engine Initialized and Ready! Enable 'Start Game' button.");
        startButton.interactable = true; // Enable the button now
    }

    // Coroutine to apply difficulty settings AFTER initialization
    IEnumerator ConfigureEngineDifficulty(int skillLevel)
    {
        if (!isEngineInitialized)
        {
            UnityEngine.Debug.LogError("Cannot configure difficulty: Engine not initialized.");
            yield break;
        }

        UnityEngine.Debug.Log("Sending difficulty configuration commands...");

        // Send commands - no need to wait for specific responses here unless debugging
        SendCommand($"setoption name Skill Level value {skillLevel}");

        // Verify configuration by checking readiness again
        bool readyOkReceived = false;
        yield return SendCommandAndWaitForResponse("isready", "readyok", 3.0f, (success) => readyOkReceived = success);

        if (readyOkReceived)
        {
            isEngineConfigured = true;
            UnityEngine.Debug.Log("Engine difficulty configured successfully and is ready.");
            needsUciNewGame = true; // Signal that the next move request should send ucinewgame
            UnityEngine.Debug.Log($"Engine difficulty configured (Skill Level {skillLevel}) and ready. Needs ucinewgame before next move.");
            // --- TRIGGER GAME START HERE ---
            // Example: Find your game manager and tell it the AI is ready
            // ShogiGameManager.Instance?.StartAiGame();
            // Or raise an event
            // OnEngineConfiguredAndReady?.Invoke();
            startButton.interactable = true; // Re-enable button if you allow re-configuration
        }
        else
        {
            isEngineConfigured = false;
            UnityEngine.Debug.LogError("Engine failed readiness check after configuration attempt.");
            startButton.interactable = true; // Re-enable button so user can try again
        }
    }

    #endregion

    #region Move Requesting

    // Public method to request a move
    public void RequestEngineMove(string currentSfen, Action<string> onMoveReceived) // Added parameter
    {
        if (!IsEngineReadyForGame) // Using the property we added before is cleaner
        {
            UnityEngine.Debug.LogError("Engine move requested, but engine is not initialized.");
            onMoveReceived?.Invoke(null); // Immediately callback with null if not ready
        }

        if (engineProcess == null || engineProcess.HasExited)
        {
            UnityEngine.Debug.LogError("Engine move requested, but process is not running or has exited.");
            StartCoroutine(HandleEngineRestart()); // Attempt to restart
            onMoveReceived?.Invoke(null); // Callback with null on process error
        }

        UnityEngine.Debug.Log("Starting GetBestMoveCoroutine...");
        StartCoroutine(GetBestMoveCoroutine(currentSfen, onMoveReceived));

        if (needsUciNewGame)
        {
            if (needsUciNewGame) // Rename this flag maybe? needsUsiNewGame?
            {
                UnityEngine.Debug.Log("Sending 'usinewgame' before setting position...");
                SendCommand("usinewgame"); // Use USI command
                needsUciNewGame = false; // Reset the flag
            }
        }
    }

    // Coroutine to handle the 'position' and 'go' commands and wait for 'bestmove'
    IEnumerator GetBestMoveCoroutine(string sfenPosition, Action<string> onMoveReceivedCallback) // Added parameter
    {
        UnityEngine.Debug.Log($"Requesting move for SFEN: position sfen {sfenPosition}");

        // Ensure previous commands are processed before sending new position/go
        bool readyOkReceived = false;
        yield return SendCommandAndWaitForResponse("isready", "readyok", 3.0f, (success) => readyOkReceived = success);
        if (!readyOkReceived)
        {
            UnityEngine.Debug.LogError("Engine not ready before sending position/go. Aborting move request.");
            yield break;
        }
        int movetime = 20;
        SendCommand($"position sfen {sfenPosition}");
        SendCommand("go movetime " + movetime);

        string bestMove = null;
        float startTime = Time.realtimeSinceStartup;
        float timeout = 2.0f; // Increased timeout for move calculation + buffer
        bool timedOut = false;
        bool engineExited = false;
        bool readError = false;
        bool loopFinishedNaturally = false;

        // --- Reading Loop (Robust Async Pattern) ---
        while (!loopFinishedNaturally && !timedOut && !engineExited && !readError)
        {
            // Check conditions & Initiate Read Task
            if (engineProcess == null || engineProcess.HasExited) { engineExited = true; break; }
            if (Time.realtimeSinceStartup - startTime > timeout) { timedOut = true; break; }

            Task<string> readTask = null;
            try { readTask = engineReader.ReadLineAsync(); }
            catch (Exception ex) { UnityEngine.Debug.LogError($"Exception initiating ReadLineAsync for bestmove: {ex.Message}"); readError = true; break; }

            // Wait for Task Completion (Yielding loop)
            while (!readTask.IsCompleted)
            {
                yield return null; // Yield execution
                if (engineProcess == null || engineProcess.HasExited) { engineExited = true; break; }
                if (Time.realtimeSinceStartup - startTime > timeout) { timedOut = true; break; }
            }
            if (engineExited || timedOut) { break; } // Exit outer loop if needed

            // Process Task Result (Try/Catch around accessing result)
            string currentLine = null;
            try
            {
                if (readTask.IsFaulted) { UnityEngine.Debug.LogError($"Error reading stream task for bestmove: {readTask.Exception}"); readError = true; }
                else { currentLine = readTask.Result; }
            }
            catch (Exception ex) { UnityEngine.Debug.LogError($"Exception accessing completed task result for bestmove: {ex.Message}"); readError = true; }
            if (readError) break;

            // Process the Line
            if (currentLine != null)
            {
                //UnityEngine.Debug.Log($"ENGINE RAW >> {currentLine}"); // Log everything
                if (currentLine.StartsWith("bestmove"))
                {
                    string[] parts = currentLine.Split(' ');
                    if (parts.Length >= 2)
                    {
                        bestMove = parts[1];
                        UnityEngine.Debug.Log($"Best move received: {bestMove}");
                        loopFinishedNaturally = true; // Found move, exit loop cleanly
                        onMoveReceivedCallback?.Invoke(bestMove);
                    }
                    else { UnityEngine.Debug.LogWarning($"Received malformed 'bestmove' line: {currentLine}"); }
                }
                // Ignore other lines (info, pv, etc.) while waiting for bestmove
            }
            else
            {
                UnityEngine.Debug.LogWarning("Engine output stream ended unexpectedly while waiting for bestmove.");
                engineExited = true; break;
            }
        } // End of while loop

        // Final Logging / Callback
        if (bestMove == null)
        {
            if (timedOut) UnityEngine.Debug.LogError("Timeout waiting for 'bestmove' response.");
            else if (engineExited) UnityEngine.Debug.LogError("Engine exited before sending 'bestmove'.");
            else if (readError) UnityEngine.Debug.LogError("Read error occurred during 'bestmove' wait. See previous logs.");
            else UnityEngine.Debug.LogError("GetBestMoveCoroutine finished without finding bestmove for unknown reasons.");

            // Attempt to recover if engine died
            if (engineProcess == null || engineProcess.HasExited)
            {
                StartCoroutine(HandleEngineRestart());
            }
            onMoveReceivedCallback?.Invoke(null);
        }
    }

    #endregion

    #region Engine Communication Helpers & Cleanup

    // Sends a command without waiting for a specific response
    void SendCommand(string command)
    {
        if (engineProcess == null || engineProcess.HasExited || engineWriter == null)
        {
            if (isEngineInitialized) // Only warn if it *should* have been working
                UnityEngine.Debug.LogWarning($"Cannot send '{command}', engine not available or already exited.");
            return; // Don't try to send if process/writer is invalid
        }

        try
        {
            // Check stream writability for extra safety
            if (!engineWriter.BaseStream.CanWrite)
            {
                UnityEngine.Debug.LogError($"Engine stream closed unexpectedly. Cannot send '{command}'.");
                StartCoroutine(HandleEngineRestart()); // Attempt restart
                return;
            }

            //UnityEngine.Debug.Log($"Sending Command: {command}"); // Verbose logging
            engineWriter.WriteLine(command);
            engineWriter.Flush(); // Ensure command is sent immediately
        }
        catch (IOException ioe)
        {
            UnityEngine.Debug.LogError($"IO Exception sending command '{command}': {ioe.Message}. Engine process likely died.");
            StartCoroutine(HandleEngineRestart()); // Attempt restart
        }
        catch (InvalidOperationException ioe) // Can happen if process exits between check and write
        {
            UnityEngine.Debug.LogError($"Invalid Operation sending command '{command}': {ioe.Message}. Engine process likely died.");
            StartCoroutine(HandleEngineRestart()); // Attempt restart
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Unexpected Exception sending command '{command}': {e.Message}\n{e.StackTrace}");
            // Consider attempting restart here too?
        }
    }

    // Sends a command and waits for a line starting with responsePrefix
    // Uses the robust async read pattern
    IEnumerator SendCommandAndWaitForResponse(string command, string responsePrefix, float waitTimeout, Action<bool> successCallback)
    {
        SendCommand(command);

        float startTime = Time.realtimeSinceStartup;
        bool responseFound = false;
        bool timedOut = false;
        bool engineExited = false;
        bool readError = false;
        bool loopFinishedNaturally = false;

        // --- Reading Loop ---
        while (!loopFinishedNaturally && !timedOut && !engineExited && !readError)
        {
            // Check conditions & Initiate Read Task
            if (engineProcess == null || engineProcess.HasExited) { engineExited = true; break; }
            if (Time.realtimeSinceStartup - startTime > waitTimeout) { timedOut = true; break; }

            Task<string> readTask = null;
            try { readTask = engineReader.ReadLineAsync(); }
            catch (Exception ex) { UnityEngine.Debug.LogError($"Exception initiating ReadLineAsync for '{responsePrefix}': {ex.Message}"); readError = true; break; }

            // Wait for Task Completion (Yielding loop)
            while (!readTask.IsCompleted)
            {
                yield return null; // Yield execution
                if (engineProcess == null || engineProcess.HasExited) { engineExited = true; break; }
                if (Time.realtimeSinceStartup - startTime > waitTimeout) { timedOut = true; break; }
            }
            if (engineExited || timedOut) { readTask?.Dispose(); break; } // Exit outer loop if needed

            // Process Task Result (Try/Catch around accessing result)
            string currentLine = null;
            try
            {
                if (readTask.IsFaulted) { UnityEngine.Debug.LogError($"Error reading stream task while waiting for '{responsePrefix}': {readTask.Exception}"); readError = true; }
                else { currentLine = readTask.Result; }
            }
            catch (Exception ex) { UnityEngine.Debug.LogError($"Exception accessing completed task result for '{responsePrefix}': {ex.Message}"); readError = true; }
            finally { readTask?.Dispose(); }
            if (readError) break;

            // Process the Line
            if (currentLine != null)
            {
                //UnityEngine.Debug.Log($"WaitRead Raw ({responsePrefix}): {currentLine}"); // Optional detailed logging
                if (currentLine.Trim().StartsWith(responsePrefix)) // Trim whitespace just in case
                {
                    responseFound = true;
                    loopFinishedNaturally = true; // Found it
                }
                // Ignore other lines while waiting for specific response
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Stream ended while waiting for '{responsePrefix}' after sending '{command}'.");
                engineExited = true; break;
            }
        } // End while loop

        // Report result
        if (!responseFound)
        {
            if (timedOut) UnityEngine.Debug.LogError($"Timeout ({waitTimeout}s) waiting for '{responsePrefix}' after sending '{command}'.");
            else if (engineExited) UnityEngine.Debug.LogError($"Engine exited before responding '{responsePrefix}' to '{command}'.");
            else if (readError) UnityEngine.Debug.LogError($"Read error occurred while waiting for '{responsePrefix}'. See previous logs.");
            else UnityEngine.Debug.LogError($"Finished waiting for '{responsePrefix}' without finding it (unknown reason).");

            // Attempt to recover if engine died
            if (engineProcess == null || engineProcess.HasExited)
            {
                StartCoroutine(HandleEngineRestart());
            }
        }
        successCallback?.Invoke(responseFound); // Callback with success status
    }

    // Handles engine cleanup
    void CleanupEngineResources()
    {
        if (engineProcess == null && engineWriter == null && engineReader == null) return; // Nothing to clean
        UnityEngine.Debug.Log("CleanupEngineResources called.");

        // Stop listening to events to prevent issues during shutdown
        if (engineProcess != null)
        {
            try { engineProcess.ErrorDataReceived -= (sender, args) => { /* Detach */ }; } catch { }
        }


        // Close streams first (order matters less here, but good practice)
        try { engineWriter?.Close(); } catch (Exception e) { UnityEngine.Debug.LogWarning($"Ignored error closing engine writer: {e.Message}"); }
        try { engineReader?.Close(); } catch (Exception e) { UnityEngine.Debug.LogWarning($"Ignored error closing engine reader: {e.Message}"); }
        engineWriter = null;
        engineReader = null;

        // Handle the process itself
        if (engineProcess != null)
        {
            try
            {
                if (!engineProcess.HasExited)
                {
                    UnityEngine.Debug.Log("Attempting graceful engine quit...");
                    bool sentQuit = false;
                    try
                    {
                        // Try sending quit command IF the stream wasn't already closed/broken
                        // Need a temporary writer instance if engineWriter was nulled above
                        using (var tempWriter = engineProcess.StandardInput)
                        {
                            if (tempWriter != null && tempWriter.BaseStream.CanWrite)
                            {
                                tempWriter.WriteLine("quit");
                                tempWriter.Flush();
                                sentQuit = true;
                            }
                            else
                            {
                                UnityEngine.Debug.LogWarning("Cannot send 'quit', input stream unavailable.");
                            }
                        }
                    }
                    catch (Exception ex) { UnityEngine.Debug.LogWarning($"Exception trying to send 'quit': {ex.Message}. Might already be closing."); }

                    // Wait briefly for graceful exit, then force kill if needed
                    if (sentQuit && engineProcess.WaitForExit(500)) // 500ms timeout
                    {
                        UnityEngine.Debug.Log("Engine exited gracefully after 'quit'.");
                    }
                    else if (!engineProcess.HasExited)
                    {
                        UnityEngine.Debug.LogWarning("Engine did not exit gracefully after 'quit' or couldn't send quit. Forcing kill.");
                        engineProcess.Kill();
                    }
                    else
                    {
                        UnityEngine.Debug.Log("Engine already exited before kill check.");
                    }
                }
                else
                {
                    UnityEngine.Debug.Log("Engine process already exited before cleanup attempt.");
                }
            }
            catch (InvalidOperationException ioex) { UnityEngine.Debug.LogWarning($"Invalid operation during process cleanup (likely already exited): {ioex.Message}"); }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error during engine process cleanup: {e.Message}");
                // Final attempt to kill if something went wrong
                try { if (engineProcess != null && !engineProcess.HasExited) engineProcess.Kill(); } catch { }
            }
            finally
            {
                engineProcess.Dispose(); // Release process resources
                engineProcess = null;
                UnityEngine.Debug.Log("Engine process disposed.");
            }
        }

        isEngineInitialized = false; // Mark as not initialized
        isEngineConfigured = false;
        if (startButton != null) startButton.interactable = false; // Disable button if UI still exists

        UnityEngine.Debug.Log("Engine resources cleanup finished.");
    }

    // Attempt to restart the engine if it dies unexpectedly
    private bool isRestarting = false;
    IEnumerator HandleEngineRestart()
    {
        if (isRestarting) yield break; // Prevent multiple restart attempts simultaneously

        isRestarting = true;
        UnityEngine.Debug.LogError("Engine process died or became unresponsive. Attempting restart...");

        CleanupEngineResources(); // Clean up the old instance thoroughly

        yield return new WaitForSeconds(1.0f); // Brief pause before restarting

        // Re-run the original initialization routine
        yield return InitializeEngineCoroutine();

        if (isEngineInitialized)
        {
            UnityEngine.Debug.Log("Engine restart successful. Re-configuration may be needed.");
            // Optionally, re-apply last known configuration automatically
            // Or just enable the start button for manual re-configuration
            if (startButton != null) startButton.interactable = true;
        }
        else
        {
            UnityEngine.Debug.LogError("Engine restart failed.");
            // Maybe disable permanently or show persistent error
            if (startButton != null) startButton.interactable = false;
        }

        isRestarting = false;
    }

    #endregion
}