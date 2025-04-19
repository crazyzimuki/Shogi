using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class that controls all highlights in the scene
public class HighLightManager : MonoBehaviour
{
    public static HighLightManager Instance { get; private set; }

    private void Awake()
    {
        // Ensure there's only one instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public static BoardGrid Grid = new BoardGrid();
    public Board b;
    public GameObject highlightPrefab;
    private List<GameObject> activeHighlights = new List<GameObject>();

    public static void ShowHighlights(List<(int row, int col)> moves, bool isDrop)
    {
        ClearHighlights();

        if (moves != null)
            foreach ((int row, int col) move in moves)
            {
                GameObject highlight = Instantiate(Instance.highlightPrefab, Grid.GetCenteredPosition(move.col, move.row), Quaternion.identity, Instance.b.LastPieceClicked.transform);
                Highlight highlightSCRIPT = highlight.GetComponent<Highlight>();

                if (isDrop)
                {
                    highlightSCRIPT.isDrop = true;
                    if (ShogiGame.Instance.shogiType != "mini")
                    highlight.transform.localScale = new Vector3(.35f, .35f, 1f);
                }
                else
                {
                    highlightSCRIPT.parentPiece = highlight.GetComponentInParent<Piece>(); // Set parentPiece of highlight
                }

                highlightSCRIPT.boardRef = Instance.b;
                highlightSCRIPT.move = move;
                Instance.activeHighlights.Add(highlight);

                if (!isDrop)
                {
                    ShogiGame.Instance.simulating = true;
                    if (highlightSCRIPT.DisableMove(ShogiGame.Instance.color)) // If move is illegal
                    {
                        Debug.Log($"Highlight for move: {highlightSCRIPT.move.row}, {highlightSCRIPT.move.col} Removed");
                        Instance.activeHighlights.Remove(highlight);
                        Destroy(highlight.gameObject);
                        continue;
                    }
                    ShogiGame.Instance.simulating = false;
                }
            }
    }

    public static void ClearHighlights()
    {
        foreach (var highlight in Instance.activeHighlights)
        {
            Destroy(highlight);
        }
        Instance.activeHighlights.Clear();
    }

    public static void DeleteHighLight(int row, int col)
    {
        foreach (var highlight in Instance.activeHighlights)
        {
            var SCRIPT = highlight.GetComponent<Highlight>();
            if (SCRIPT.move.row == row && SCRIPT.move.col == col)
            {
                Instance.activeHighlights.Remove(highlight);
                Destroy(highlight);
            }
        }
    }

    public bool VerifyLegality(Highlight bro)
    {
        if ((bro.parentPiece.data.color == 1 && Instance.b.data.board[bro.parentPiece.data.row, bro.parentPiece.data.col] < 0) || (bro.parentPiece.data.color == -1 && Instance.b.data.board[bro.parentPiece.data.row, bro.parentPiece.data.col] > 0)) // Trying to take own piece
        {
            Destroy(bro);
            return true; // bro destroyed
        }
        return false; // not destroyed
    }

    public static Highlight GetHighlight(int row, int col)
    {
        foreach (GameObject highlight in Instance.activeHighlights)
        {
            Highlight highlightSCRIPT = highlight.GetComponent<Highlight>();
            if (highlightSCRIPT.move.row == row && highlightSCRIPT.move.col == col)
            {
                return highlightSCRIPT;
            }
        }
        return null;
    }
}