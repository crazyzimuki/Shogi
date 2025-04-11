using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Board : MonoBehaviour
{
    public BoardDATA data = new BoardDATA();
    public List<Piece> Pieces = new List<Piece>();
    public GameObject LastPieceClicked;

    public GameObject DropsUI;       // (Assume implemented)
    public GameObject DroppablePiece; // Prefab for drops

    public GameObject King;
    public GameObject Rook;
    public GameObject Bishop;
    public GameObject Gold;
    public GameObject Silver;
    public GameObject Pawn;

    private void Awake()
    {
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
            default: return null;
        }
    }

    public void PrintBoard()
    {
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                SpawnPieceType(data.board[i, j], i, j);
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

        instance.transform.position += new Vector3(0.022f, comp.data.color * 0.09f, 0f);
        if (comp.data.color < 0)
            instance.transform.position += new Vector3(0f, -0.47f, 0f);
        return comp;
    }

    public void CreateNewDrop(PieceDATA pieceDATA)
    {
        if (pieceDATA == null)
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

        // Position based on existing drops of this color
        int positionIndex = data.AllDroppedPiecesDataOfColor(dropData.color).Count - 1; // -1 because we just added it
        drop.transform.localPosition = new Vector3(2.227f + positionIndex * 0.4f, -1.651f, 0f);
        drop.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
        SpriteRenderer rend = drop.GetComponent<SpriteRenderer>();
        rend.flipY = (dropData.color < 0); // Flip if black
        drop.ResetCollider();
    }

    public void CreateNewDrop(Piece pieceToCapture)
    {
        if (pieceToCapture == null)
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
        int positionIndex = data.AllDroppedPiecesDataOfColor(dropData.color).Count - 1; // -1 because we just added it
        drop.transform.localPosition = new Vector3(2.227f + positionIndex * 0.4f, -1.651f, 0f);
        drop.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
        SpriteRenderer rend = drop.GetComponent<SpriteRenderer>();
        rend.flipY = (dropData.color < 0); // Flip if black
        drop.ResetCollider();
    }

    public PieceDATA CreatePieceDataByType(int pieceType)
    {
        switch (pieceType)
        {
            case 7: return new King();
            case 5: return new Rook();
            case 4: return new Bishop();
            case 3: return new Gold();
            case 2: return new Silver();
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


//// ONE BIG BUG REMAINING: DROPS A DIFFERENT PIECE THAN ONE I CLICKED. REDO THAT PART. REDO DROPS UI TOO IT SOMETIMES HAS OVERLAPPING PIECES.