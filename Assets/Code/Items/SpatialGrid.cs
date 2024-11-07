using System.Collections.Generic;
using UnityEngine;

public class SpatialGrid : MonoBehaviour
{
    private float cellSize;
    private Dictionary<Vector2Int, HashSet<GameObject>> grid;

    public SpatialGrid(float cellSize)
    {
        this.cellSize = cellSize;
        grid = new Dictionary<Vector2Int, HashSet<GameObject>>();
    }

    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / cellSize),
            Mathf.FloorToInt(worldPosition.z / cellSize)
        );
    }

    public void AddItem(GameObject item)
    {
        Vector2Int gridPos = GetGridPosition(item.transform.position);
        if (!grid.ContainsKey(gridPos))
            grid[gridPos] = new HashSet<GameObject>();

        grid[gridPos].Add(item);
    }

    public void RemoveItem(GameObject item)
    {
        Vector2Int gridPos = GetGridPosition(item.transform.position);
        if (grid.ContainsKey(gridPos))
            grid[gridPos].Remove(item);
    }

    public List<GameObject> GetItemsInRadius(Vector3 position, float radius)
    {
        List<GameObject> result = new List<GameObject>();
        Vector2Int centerGrid = GetGridPosition(position);
        int gridRadius = Mathf.CeilToInt(radius / cellSize);

        for (int x = -gridRadius; x <= gridRadius; x++)
        {
            for (int z = -gridRadius; z <= gridRadius; z++)
            {
                Vector2Int checkPos = centerGrid + new Vector2Int(x, z);
                if (grid.ContainsKey(checkPos))
                    result.AddRange(grid[checkPos]);
            }
        }

        return result;
    }
}
