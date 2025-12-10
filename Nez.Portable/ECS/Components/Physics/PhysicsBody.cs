using Microsoft.Xna.Framework;

namespace Nez
{
    /// <summary>
    /// Simple physics body that applies its velocity each frame using an IMover on the same Entity. Its update order is set to int.MaxValue
    /// on the constructor so it runs after most other logic.
    /// </summary>
    public class PhysicsBody : Component, IUpdatable
    {
        [Inspectable] public Vector2 Velocity;
        public bool MoveOnUpdate = true;

        IMover _mover;

        public PhysicsBody(){
            UpdateOrder = int.MaxValue;
        }

        public override void OnAddedToEntity()
        {
            _mover = Entity.GetComponent<IMover>();
            Debug.WarnIf(_mover == null, "PhysicsBody requires an IMover on the same Entity.");
        }

        public void Update()
        {
            if (!MoveOnUpdate || _mover == null)
                return;

            var motion = CalculateMotion();
            _mover.Move(motion);
        }

        public virtual Vector2 CalculateMotion() => Velocity * Time.DeltaTime;
    }
}
