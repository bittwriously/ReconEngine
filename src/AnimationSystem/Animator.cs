using System.Numerics;
using ReconEngine.WorldSystem;

namespace ReconEngine.AnimationSystem;

public class Animator : ReconEntity
{
    public AnimationController? Controller;
    private readonly List<AnimationTrack> _tracks = [];

    public AnimationTrack LoadAnimation(AnimationClip clip)
    {
        var track = new AnimationTrack(clip);
        //track.Ended += () => _tracks.Remove(track);
        _tracks.Add(track);
        return track;
    }

    public override void PhysicsStep(float deltaTime)
    {
        base.PhysicsStep(deltaTime);

        if (Controller == null) return;
        var activePoses = new List<(int priority, float weight, Dictionary<string, (Vector3 pos, Quaternion rot)> pose)>();

        foreach (var track in _tracks.ToList())
        {
            if (!track.IsPlaying) continue;
            var pose = track.Advance(deltaTime);
            if (pose == null || pose.Count == 0) continue;
            activePoses.Add((track.Priority, track.Weight, pose));
        }

        var motorTopPriority = new Dictionary<string, int>();

        foreach (var (priority, _, pose) in activePoses)
        {
            foreach (var name in pose.Keys)
            {
                if (!motorTopPriority.TryGetValue(name, out int current) || priority > current)
                    motorTopPriority[name] = priority;
            }
        }

        var blended = new Dictionary<string, (Vector3 pos, Quaternion rot, float weight)>();

        foreach (var (priority, weight, pose) in activePoses)
        {
            foreach (var (name, (pos, rot)) in pose)
            {
                if (motorTopPriority[name] != priority) continue;

                if (blended.TryGetValue(name, out var existing))
                {
                    float totalWeight = existing.weight + weight;
                    float t = weight / totalWeight;

                    blended[name] = (
                        Vector3.Lerp(existing.pos, pos, t),
                        Quaternion.Slerp(existing.rot, rot, t),
                        totalWeight
                    );
                }
                else blended[name] = (pos, rot, weight);
            }
        }

        foreach (var (name, (pos, rot, _)) in blended)
        {
            if (Controller.Joints.TryGetValue(name, out var motor))
            {
                motor.AnimPosition = pos;
                motor.AnimRotation = rot;
            }
        }

        foreach (var (name, joint) in Controller.Joints)
        {
            if (!blended.ContainsKey(name))
            {
                joint.AnimPosition = Vector3.Zero;
                joint.AnimRotation = Quaternion.Identity;
            }
        }
    }
}
