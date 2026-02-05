using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Numerics;

namespace ReconEngine.UISystem.UIGrid;

// this tool is made so we dont
// check for each object on each
// mouse input since that would be
// unreasonably wasteful
public class GridCell(GuiGrid2D parent, int x, int y)
{
    public readonly int X = x;
    public readonly int Y = y;
    public readonly long Key = parent.GetKeyOf(x, y);

    public readonly Vector2 MinBounds = new Vector2(x, y) * parent.GridSize;
    public readonly Vector2 MaxBounds = new Vector2(x+1, y+1) * parent.GridSize;

    private readonly List<GuiObject> _objects = [];
    private List<GuiObject> _sortedObjects = [];

    private void SortObjects() => _sortedObjects = [.. _objects.OrderBy(c => c.ZIndex)];

    public void AddObject(GuiObject obj)
    {
        _objects.Add(obj);
        SortObjects();
    }
    public void RemoveObject(GuiObject obj)
    {
        _objects.Remove(obj);
        SortObjects();
    }
    public ReadOnlyCollection<GuiObject> GetList() => _sortedObjects.AsReadOnly();
}

public class GuiGrid2D
{
    public readonly int GridSize;
    private readonly float _invGridSize;
    private Vector2 _screenSize;
    
    private Dictionary<long, GridCell> _gridCells = [];

    private Dictionary<uint, List<long>> _registeredCells = [];

    public GuiGrid2D(Vector2 screenSize, int gridSize = 128)
    {
        GridSize = gridSize;
        _invGridSize = 1.0f / gridSize;
        _screenSize = screenSize;

        RegenerateGridBuffer(screenSize);        
    }

    public void ResizeGridBuffer(Vector2 screenSize)
    {
        int newXCount = (int)MathF.Ceiling(screenSize.X * _invGridSize);
        int newYCount = (int)MathF.Ceiling(screenSize.Y * _invGridSize);
        int oldXCount = (int)MathF.Ceiling(_screenSize.X * _invGridSize);
        int oldYCount = (int)MathF.Ceiling(_screenSize.Y * _invGridSize);
        if (newXCount < oldXCount || newYCount < oldYCount)
        {
            var keysToRemove = _gridCells.Keys
                .Where(key => ExtractX(key) > newXCount || ExtractY(key) > newYCount)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _gridCells.Remove(key);
            }
        }
        for (int x = 0; x < newXCount; x++)
        {
            for (int y = 0; y < newYCount; y++)
            {
                long key = GetKeyOf(x, y);
                if (!_gridCells.ContainsKey(key))
                {
                    _gridCells[key] = new GridCell(this, x, y);
                }
            }
        }
        _screenSize = screenSize;
    }

    public void RegenerateGridBuffer(Vector2 screenSize)
    {
        int xGridCount = (int)MathF.Ceiling(screenSize.X * _invGridSize);
        int yGridCount = (int)MathF.Ceiling(screenSize.Y * _invGridSize);
        for (int x = 0; x < xGridCount; x++)
        {
            for (int y = 0; y < yGridCount; y++)
            {
                GridCell cell = new(this, x, y);
                _gridCells[cell.Key] = cell;
            }
        }
    }

    public long GetKeyOf(Vector2 vector) => GetKeyOf((int)vector.X, (int)vector.Y);
    public long GetKeyOf(int x, int y)
    {
        return (uint)x | ((long)(uint)y << 32);
    }

    public int ExtractX(long key) => (int)(uint)(key & 0xFFFFFFFF);
    public int ExtractY(long key) => (int)(uint)((key >> 32) & 0xFFFFFFFF);

    private (Vector2 min, Vector2 max) GetAABB(GuiObject obj) 
    {
        Vector2 pos = new(obj.TransformCache.PosX, obj.TransformCache.PosY);
        Vector2 size = new(obj.TransformCache.SizeX, obj.TransformCache.SizeY);
        if (obj.Rotation == 0) {
            return (pos, pos + size);
        }
        Vector2[] corners = [
            Vector2.Zero,               // top-left
            new(size.X, 0),             // top-right
            new(0, size.Y),             // bottom-left
            size                        // bottom-right
        ];
        float rad = obj.Rotation * (MathF.PI / 180f);
        float cos = MathF.Cos(rad);
        float sin = MathF.Sin(rad);
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        foreach (var corner in corners) {
            float rotatedX = pos.X + (corner.X * cos - corner.Y * sin);
            float rotatedY = pos.Y + (corner.X * sin + corner.Y * cos);
            minX = MathF.Min(minX, rotatedX);
            minY = MathF.Min(minY, rotatedY);
            maxX = MathF.Max(maxX, rotatedX);
            maxY = MathF.Max(maxY, rotatedY);
        }
        return (new Vector2(minX, minY), new Vector2(maxX, maxY));
    }

    public void RegisterObject(GuiObject obj)
    {
        var (min, max) = GetAABB(obj);
        int minX = (int)MathF.Floor(min.X * _invGridSize);
        int maxX = (int)MathF.Floor(max.X * _invGridSize);
        int minY = (int)MathF.Floor(min.Y * _invGridSize);
        int maxY = (int)MathF.Floor(max.Y * _invGridSize);
        List<long> occupiedKeys = [];
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                long key = GetKeyOf(x, y);
                if (_gridCells.TryGetValue(key, out var cell))
                {
                    cell.AddObject(obj);
                    occupiedKeys.Add(key);
                }
            }
        }
        _registeredCells[obj.EntityId] = occupiedKeys;
    }

    public void UnregisterObject(GuiObject obj)
    {
        if (_registeredCells.Remove(obj.EntityId, out var keys)) foreach (var key in keys) if (_gridCells.TryGetValue(key, out var cell)) cell.RemoveObject(obj);
    }

    public void UpdateObject(GuiObject obj)
    {
        UnregisterObject(obj);
        RegisterObject(obj);
    }

}