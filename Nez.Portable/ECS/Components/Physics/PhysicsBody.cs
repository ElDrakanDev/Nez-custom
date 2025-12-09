using Microsoft.Xna.Framework;

namespace Nez
{
    /// <summary>
    /// Simple physics body that applies its velocity each frame using an IMover on the same Entity.
    /// </summary>
    public class PhysicsBody : Component, IUpdatable
    {
        [Inspectable]
        public Vector2 Velocity;
        public bool MoveOnUpdate = true;

        IMover _mover;

        public override void OnAddedToEntity()
        {
            _mover = Entity.GetComponent<IMover>();
            Debug.WarnIf(_mover == null, "PhysicsBody requires an IMover on the same Entity.");
        }

        public void Update()
        {
            if (!MoveOnUpdate || _mover == null)
                return;

            var motion = Velocity * Time.DeltaTime;
            _mover.Move(motion);
        }
    }
}
