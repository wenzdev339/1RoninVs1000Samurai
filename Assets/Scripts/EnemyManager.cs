using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("Spatial Partitioning")]
    [SerializeField] private int spatialGridWidth = 100;
    [SerializeField] private int spatialGridHeight = 100;
    [SerializeField] private int numberOfPartitions = 10000;
    
    public Dictionary<int, HashSet<Enemy>> enemySpatialGroups = new Dictionary<int, HashSet<Enemy>>();
    
    private int cellsPerRow;
    private int cellsPerColumn;
    private float cellWidth;
    private float cellHeight;
    private int halfWidth;
    private int halfHeight;

    private List<Enemy> allActiveEnemies = new List<Enemy>();
    public List<Enemy> AllActiveEnemies => allActiveEnemies;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeSpatialPartitioning();
    }

    private void InitializeSpatialPartitioning()
    {
        cellsPerRow = (int)Mathf.Sqrt(numberOfPartitions);
        cellsPerColumn = cellsPerRow;
        cellWidth = (float)spatialGridWidth / cellsPerRow;
        cellHeight = (float)spatialGridHeight / cellsPerColumn;
        halfWidth = spatialGridWidth / 2;
        halfHeight = spatialGridHeight / 2;
        
        for (int i = 0; i < numberOfPartitions; i++)
        {
            enemySpatialGroups.Add(i, new HashSet<Enemy>());
        }
    }

    public int GetSpatialGroup(float xPos, float yPos)
    {
        float adjustedX = xPos + halfWidth;
        float adjustedY = yPos + halfHeight;

        int xIndex = Mathf.Clamp((int)(adjustedX / cellWidth), 0, cellsPerRow - 1);
        int yIndex = Mathf.Clamp((int)(adjustedY / cellHeight), 0, cellsPerColumn - 1);

        return xIndex + yIndex * cellsPerRow;
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (!allActiveEnemies.Contains(enemy))
        {
            allActiveEnemies.Add(enemy);
        }
        
        int spatialGroup = GetSpatialGroup(enemy.transform.position.x, enemy.transform.position.y);
        AddToSpatialGroup(spatialGroup, enemy);
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        allActiveEnemies.Remove(enemy);
        
        if (enemy.spatialGroup >= 0 && enemySpatialGroups.ContainsKey(enemy.spatialGroup))
        {
            RemoveFromSpatialGroup(enemy.spatialGroup, enemy);
        }
    }

    public void AddToSpatialGroup(int spatialGroupID, Enemy enemy)
    {
        if (enemySpatialGroups.ContainsKey(spatialGroupID))
        {
            enemySpatialGroups[spatialGroupID].Add(enemy);
        }
    }

    public void RemoveFromSpatialGroup(int spatialGroupID, Enemy enemy)
    {
        if (enemySpatialGroups.ContainsKey(spatialGroupID))
        {
            enemySpatialGroups[spatialGroupID].Remove(enemy);
        }
    }

    public List<Enemy> GetNearbyEnemies(Vector2 position, float radius)
    {
        List<Enemy> nearbyEnemies = new List<Enemy>();
        
        int centerGroup = GetSpatialGroup(position.x, position.y);
        int radiusInCells = Mathf.CeilToInt(radius / cellWidth) + 1;
        List<int> groupsToCheck = GetExpandedSpatialGroups(centerGroup, radiusInCells);
        
        float sqrRadius = radius * radius;
        
        foreach (int groupID in groupsToCheck)
        {
            if (!enemySpatialGroups.ContainsKey(groupID))
                continue;
                
            foreach (Enemy enemy in enemySpatialGroups[groupID])
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy)
                    continue;
                    
                float sqrDistance = ((Vector2)enemy.transform.position - position).sqrMagnitude;
                
                if (sqrDistance <= sqrRadius)
                {
                    nearbyEnemies.Add(enemy);
                }
            }
        }
        
        return nearbyEnemies;
    }

    private List<int> GetExpandedSpatialGroups(int spatialGroup, int radius = 1)
    {
        List<int> expandedGroups = new List<int>();

        int centerRow = spatialGroup / cellsPerRow;
        int centerCol = spatialGroup % cellsPerRow;

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                int newRow = centerRow + dy;
                int newCol = centerCol + dx;
                
                if (newRow >= 0 && newRow < cellsPerColumn && newCol >= 0 && newCol < cellsPerRow)
                {
                    int newGroup = newCol + newRow * cellsPerRow;
                    expandedGroups.Add(newGroup);
                }
            }
        }

        return expandedGroups;
    }

    public int SpatialGridWidth => spatialGridWidth;
    public int SpatialGridHeight => spatialGridHeight;
    public int NumberOfPartitions => numberOfPartitions;
}