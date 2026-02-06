namespace ReconEngine.WorldSystem;

public interface IUpdatable
{
    public virtual void RenderStep(float deltaTime) { }
    public virtual void PhysicsStep(float deltaTime) { }
}
