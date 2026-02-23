using System.Collections.ObjectModel;
using System.Numerics;

namespace ReconEngine.PhysicsHandler;

public class CollisionGroupRegistry
{
    private readonly Dictionary<string, ulong> _groups = [];
    private readonly Dictionary<string, int> _groupIndex = [];
    private readonly Queue<int> _freeBits = new();
    private int _nextBit = 0;
    private readonly Dictionary<ulong, List<IPhysicsBody>> _registeredBodies = [];
    private readonly ulong[] _matrix = new ulong[64];

    public ulong Register(string name)
    {
        if (_groups.TryGetValue(name, out ulong existing))
            return existing;
        int bit = _freeBits.Count > 0 ? _freeBits.Dequeue() : _nextBit++;
        if (bit >= 64)
            throw new InvalidOperationException("maximum of 64 collision groups reached");
        ulong mask = 1UL << bit;
        _groups[name] = mask;
        _groupIndex[name] = bit;
        _registeredBodies.Add(mask, []);
        _matrix[bit] = ulong.MaxValue;
        return mask;
    }

    public void Unregister(string name)
    {
        if (!_groups.TryGetValue(name, out ulong mask))
            return;
        _groups.Remove(name);
        _groupIndex.Remove(name);
        int bit = BitOperations.TrailingZeroCount(mask);
        _freeBits.Enqueue(bit);
        _registeredBodies.Remove(mask);
        ulong bitMask = 1UL << bit;
        _matrix[bit] = 0;
        for (int i = 0; i < 64; i++)
            _matrix[i] &= ~bitMask;
    }

    public void SetCollision(string groupA, string groupB, bool collides)
    {
        int a = GetIndex(groupA);
        int b = GetIndex(groupB);

        if (collides)
        {
            _matrix[a] |=  1UL << b;
            _matrix[b] |=  1UL << a;
        }
        else
        {
            _matrix[a] &= ~(1UL << b);
            _matrix[b] &= ~(1UL << a);
        }
    }

    public void SetCollisionOneWay(string groupA, string groupB, bool collides)
    {
        int a = GetIndex(groupA);
        int b = GetIndex(groupB);

        if (collides)
            _matrix[a] |=  1UL << b;
        else
            _matrix[a] &= ~(1UL << b);
    }

    public bool Collides(int bitA, int bitB)
    {
        return (_matrix[bitA] & (1UL << bitB)) != 0;
    }

    public bool Collides(string nameA, string nameB)
    {
        int a = GetIndex(nameA);
        int b = GetIndex(nameB);
        return (_matrix[a] & (1UL << b)) != 0;
    }

    public void RegisterBody(IPhysicsBody physicsBody, string name)
    {
        if (!_groups.TryGetValue(name, out ulong bit))
            throw new KeyNotFoundException($"collision group '{name}' is not registered");
        if (!_registeredBodies.TryGetValue(bit, out List<IPhysicsBody>? list))
            throw new Exception($"collision group body registry for '{name}' is missing");
        list.Add(physicsBody);
    }

    public void UnregisterBody(IPhysicsBody physicsBody)
    {
        if (!_groups.TryGetValue(physicsBody.CollisionGroup, out ulong bit))
            throw new KeyNotFoundException($"collision group '{physicsBody.CollisionGroup}' is not registered");
        if (!_registeredBodies.TryGetValue(bit, out List<IPhysicsBody>? list))
            throw new Exception($"collision group body registry for '{physicsBody.CollisionGroup}' is missing");
        list.Remove(physicsBody);
    }

    public ulong Get(string name)
    {
        if (!_groups.TryGetValue(name, out ulong bit))
            throw new KeyNotFoundException($"collision group '{name}' is not registered");
        return bit;
    }

    public ulong Mask(params string[] names)
    {
        ulong mask = 0;
        foreach (var name in names)
            mask |= Get(name);
        return mask;
    }

    public bool TryGet(string name, out ulong bit) => _groups.TryGetValue(name, out bit);

    public ReadOnlyCollection<IPhysicsBody> GetGroupBodies(string name)
    {
        if (!_groups.TryGetValue(name, out ulong bit))
            throw new KeyNotFoundException($"collision group '{name}' is not registered");
        if (!_registeredBodies.TryGetValue(bit, out List<IPhysicsBody>? list))
            throw new Exception($"collision group body registry for '{name}' is missing");
        return list.AsReadOnly();
    }

    public int GetIndex(string name)
    {
        if (!_groupIndex.TryGetValue(name, out int bit))
            throw new KeyNotFoundException($"Collision group '{name}' not registered.");
        return bit;
    }

    public bool TryGetIndex(string name, out int bit)
        => _groupIndex.TryGetValue(name, out bit);
}