using Microsoft.Xna.Framework;

namespace Nez
{
    /// <summary>
    /// Abstraction for components that can move an Entity while accounting for collisions.
    /// </summary>
    public interface IMover
    {
        /// <summary>
        /// Moves the Entity by the given motion vector. Returns true when any collision is detected/handled.
        /// </summary>
        /// <param name="motion">Desired movement delta.</param>
        bool Move(Vector2 motion);
    }
}
