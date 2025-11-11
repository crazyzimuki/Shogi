using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Board : MonoBehaviour
{
    public string gameType;
    public static string shogiType;
    public BoardDATA data = new BoardDATA(true, Board.shogiType);
    public List<Piece> Pieces = new List<Piece>();
    public GameObject LastPieceClicked;

    public GameObject DropsUI;
    public GameObject DroppablePiece; // Prefab for drops

    public GameObject King;
    public GameObject Rook;
    public GameObject Bishop;
    public GameObject Gold;
    public GameObject Silver;
    public GameObject Pawn;
    public GameObject Horse;
    public GameObject Lance;

    private void Awake()
    {
        shogiType = gameType;
        data.InitializeBoard();
        data.boardRef = this;
        LastPieceClicked = gameObject;
    }

    public Piece SpawnPieceType(int piece, int row, int col)
    {
        switch (Mathf.Abs(piece))
        {
            case 7: return SpawnPiece(row, col, King);
            case 5: return SpawnPiece(row, col, Rook);
            case 4: return SpawnPiece(row, col, Bishop);
            case 3: return SpawnPiece(row, col, Gold);
            case 2: return SpawnPiece(row, col, Silver);
            case 1: return SpawnPiece(row, col, Pawn);
            case 8: return SpawnPiece(row, col, Horse);
            case 9: return SpawnPiece(row, col, Lance);
            default: return null;
        }
    }

    public void PrintBoard()
    {
        if (ShogiGame.Instance.shogiType == "mini")
        {
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                    SpawnPieceType(data.board[i, j], i, j);
        }

        else if (ShogiGame.Instance.shogiType == "chu")
        {
            for (int i = 0; i < 12; i++)
                for (int j = 0; j < 12; j++)
                    SpawnPieceType(data.board[i, j], i, j);
        }

        else
        {
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    SpawnPieceType(data.board[i, j], i, j);
        }
    }

    public Piece SpawnPiece(int row, int col, GameObject piecePrefab)
    {
        if (this == null) { ShogiGame.EndLife(-1); return null; }
        GameObject instance = Instantiate(piecePrefab, HighLightManager.Grid.GetWorldPosition(col, row), Quaternion.identity, transform);
        Piece comp = instance.GetComponent<Piece>();

        int pieceType = Mathf.Abs(data.board[row, col]);
        comp.data = CreatePieceDataByType(pieceType);

        comp.data.pieceId = PieceDATA.nextPieceId++;
        comp.data.color = (int)Mathf.Sign(data.board[row, col]);
        comp.data.pieceType = pieceType;
        comp.data.row = row;
        comp.data.col = col;
        comp.data.boardRef = this;
        comp.data.pieceRef = comp;

        Pieces.Add(comp);
        data.Pieces.Add(comp.data);

        if (ShogiGame.Instance.shogiType != "mini")
        {
            comp.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        }
        instance.transform.position += new Vector3(0.022f, comp.data.color * 0.09f, 0f);

        if (comp.data.color < 0)
        {
            if (ShogiGame.Instance.shogiType == "mini")
            instance.transform.position += new Vector3(0f, -0.47f, 0f);
            else
            instance.transform.localPosition += new Vector3(0f, -0.75f, 0f);
        }
        return comp;
    }

    public void CreateNewDrop(PieceDATA pieceDATA)
    {
        if (pieceDATA == null || shogiType == "chu") // No drops in chu-shogi
            return;

        DroppedPieceDATA dropData = new DroppedPieceDATA();
        dropData.pieceType = pieceDATA.pieceType;
        dropData.color = -pieceDATA.color; // Capturer’s color
        dropData.promoted = false;
        dropData.boardRef = this;

        // Add to data list before positioning for correct count
        data.droppedPiecesData.Add(dropData);

        GameObject dropObj = Instantiate(DroppablePiece);
        DroppedPiece drop = dropObj.GetComponent<DroppedPiece>();
        drop.data = dropData;
        drop.Init(this, dropData.pieceType, dropData.color, pieceDATA.pieceRef.DefaultSPR, false);

        // Remove null entries
        foreach (DroppedPieceDATA obj in data.droppedPiecesData)
        {
            if (obj == null)
                data.droppedPiecesData.Remove(obj);
        }
    }

    // IMPROVE THIS METHOD. STACK PIECES OF SAME TYPE!
    public void ReorganizeDrops() 
    {
        int positionIndex = 0;
        foreach (DroppedPieceDATA dpr in data.AllDroppedPiecesDataOfColor(1))
        {
            GameObject drop = dpr.parent;
            DroppedPiece droop = drop.GetComponent<DroppedPiece>();
            if (positionIndex < 6)
            drop.transform.localPosition = new Vector3(2.227f + positionIndex * 0.3f, -1.1f, 0f);
            else
            {
                drop.transform.localPosition = new Vector3(2.227f + (positionIndex % 6) * 0.3f, -1.1f + (-0.4f * (positionIndex/6)), 0f);
            }

            drop.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
            SpriteRenderer rend = drop.GetComponent<SpriteRenderer>();
            rend.flipY = (dpr.color < 0); // Flip if black
            droop.ResetCollider();
            positionIndex++;
        }
        positionIndex = 0;
        foreach (DroppedPieceDATA dpr in data.AllDroppedPiecesDataOfColor(-1))
        {
            GameObject drop = dpr.parent;
            DroppedPiece droop = drop.GetComponent<DroppedPiece>();
            if (positionIndex < 6)
                drop.transform.localPosition = new Vector3(2.227f + positionIndex * 0.3f, -1.1f, 0f);
            else
            {
                drop.transform.localPosition = new Vector3(2.227f + (positionIndex % 6) * 0.3f, -1.1f + (0.4f * (positionIndex / 6)), 0f);
            }

            drop.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
            SpriteRenderer rend = drop.GetComponent<SpriteRenderer>();
            rend.flipY = (dpr.color < 0); // Flip if black
            droop.ResetCollider();
            positionIndex++;
        }
    }

    public void CreateNewDrop(Piece pieceToCapture)
    {
        if (pieceToCapture == null || shogiType == "chu") // No drops in chu-shogi
            return;

        DroppedPieceDATA dropData = new DroppedPieceDATA();
        dropData.pieceType = pieceToCapture.data.pieceType;
        dropData.color = -pieceToCapture.data.color; // Capturer’s color
        dropData.promoted = false;
        dropData.boardRef = this;

        // Add to data list before positioning for correct count
        data.droppedPiecesData.Add(dropData);

        GameObject dropObj = Instantiate(DroppablePiece);
        DroppedPiece drop = dropObj.GetComponent<DroppedPiece>();
        drop.data = dropData;
        dropData.parent = dropObj;
        drop.Init(this, dropData.pieceType, dropData.color, pieceToCapture.DefaultSPR, false);

        // Position based on existing drops of this color
        ReorganizeDrops();
    }

    public PieceDATA CreatePieceDataByType(int pieceType)
    {
        switch (pieceType)
        {
            case 13: return new Chariot();
            case 14: return new Tiger();
            case 15: return new Phoenix(false);
            case 16: return new Kirin(false);
            case 17: return new Copper(true); // Crab
            case 18: return new Silver(true); // Snake
            case 19: return new Bishop(true); // Dragonhorse
            case 20: return new Rook(true); // Dragonking
            case 21: return new Phoenix(true); // Queen
            case 22: return new Kirin(true); // Lion
            case 23: return new DoublePawn();
            case 11: return new Copper(false);
            case 10: return new Leopard();
            case 9: return new Lance();
            case 8: return new Horse();
            case 7: return new King();
            case 5: return new Rook(false);
            case 4: return new Bishop(false);
            case 3: return new Gold();
            case 2: return new Silver(false);
            case 1: return new Pawn();
            default: return null;
        }
    }

    public static void DestroyPiece(GameObject obj)
    {
        Destroy(obj);
    }

    public Piece PieceAt(int row, int col)
    {
        return Pieces.FirstOrDefault(p => p.data.row == row && p.data.col == col);
    }

    public Board CopyBoard()
    {
        Board copy = new Board();
        copy.data = data.Clone();
        copy.data.boardRef = copy;
        copy.King = King;
        copy.Rook = Rook;
        copy.Bishop = Bishop;
        copy.Gold = Gold;
        copy.Silver = Silver;
        copy.Pawn = Pawn;
        return copy;
    }
}