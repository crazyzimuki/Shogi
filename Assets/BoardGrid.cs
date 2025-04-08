using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class that converts board coordinates to world positions
public class BoardGrid
{
    public int gridSize = 5; // 5x5 grid
    public float cellSize = 0.425f; // Adjust this to control the size of the grid cells
    public Vector3 Offset = Vector3.zero;


    // Method to convert 2D array coordinates to world position
    public Vector3 GetWorldPosition(int x, int y)
    {
        // Note: y is inverted because the origin is top-left
        return new Vector3(x * cellSize, -y * cellSize, 0f) + Offset;
    }

    public Vector3 GetCenteredPosition(int x, int y)
    {
        // Calculate the top-left corner position
        Vector3 topLeftCorner = GetWorldPosition(x, y);

        // Add half the cell size to x and subtract half the cell size from y to get the center
        return topLeftCorner + new Vector3(cellSize / 2, -cellSize / 2, 0f);
    }

    // Visualize the grid in the Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Vector3 pos = GetWorldPosition(x, y);
                // Center the wireframe so origin is at top left of grid
                Gizmos.DrawWireCube(pos + new Vector3(cellSize / 2, -cellSize / 2, 0), new Vector3(cellSize, cellSize, 0));
            }
        }
    }
}
