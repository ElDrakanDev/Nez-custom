using Nez.Tiled;
using Microsoft.Xna.Framework;
using System.Collections.Generic;


namespace Nez
{
	public class TiledMapRenderer : RenderableComponent, IUpdatable
	{
		public TmxMap TiledMap;

		public int PhysicsLayer = 1 << 0;

		/// <summary>
		/// if null, all layers will be rendered
		/// </summary>
		public int[] LayerIndicesToRender;

		public bool AutoUpdateTilesets = true;

		public override float Width => TiledMap.Width * TiledMap.TileWidth;
		public override float Height => TiledMap.Height * TiledMap.TileHeight;

		public TmxLayer CollisionLayer;

		bool _shouldCreateColliders;
		Collider[] _colliders;


		public TiledMapRenderer(TmxMap tiledMap, string collisionLayerName = null, bool shouldCreateColliders = true)
		{
			TiledMap = tiledMap;

			_shouldCreateColliders = shouldCreateColliders;

			if (collisionLayerName != null)
				CollisionLayer = tiledMap.TileLayers[collisionLayerName];
		}

		/// <summary>
		/// sets this component to only render a single layer
		/// </summary>
		/// <param name="layerName">Layer name.</param>
		public void SetLayerToRender(string layerName)
		{
			LayerIndicesToRender = new int[1];
			LayerIndicesToRender[0] = TiledMap.Layers.IndexOf(TiledMap.GetLayer(layerName));
		}

		/// <summary>
		/// sets which layers should be rendered by this component by name. If you know the indices you can set layerIndicesToRender directly.
		/// </summary>
		/// <param name="layerNames">Layer names.</param>
		public void SetLayersToRender(params string[] layerNames)
		{
			LayerIndicesToRender = new int[layerNames.Length];

			for (var i = 0; i < layerNames.Length; i++)
				LayerIndicesToRender[i] = TiledMap.Layers.IndexOf(TiledMap.GetLayer(layerNames[i]));
		}


		#region TiledMap queries

		public int GetRowAtWorldPosition(float yPos)
		{
			yPos -= Entity.Transform.Position.Y + _localOffset.Y;
			return TiledMap.WorldToTilePositionY(yPos);
		}

		public int GetColumnAtWorldPosition(float xPos)
		{
			xPos -= Entity.Transform.Position.X + _localOffset.X;
			return TiledMap.WorldToTilePositionX(xPos);
		}

		/// <summary>
		/// this method requires that you are using a collision layer setup in the constructor.
		/// </summary>
		public TmxLayerTile GetTileAtWorldPosition(Vector2 worldPos)
		{
			Insist.IsNotNull(CollisionLayer, "collisionLayer must not be null!");

			// offset the passed in world position to compensate for the entity position
			worldPos -= Entity.Transform.Position + _localOffset;

			return CollisionLayer.GetTileAtWorldPosition(worldPos);
		}

		/// <summary>
		/// gets all the non-empty tiles that intersect the passed in bounds for the collision layer. The returned List can be put back in the
		/// pool via ListPool.free.
		/// </summary>
		/// <returns>The tiles intersecting bounds.</returns>
		/// <param name="bounds">Bounds.</param>
		public List<TmxLayerTile> GetTilesIntersectingBounds(Rectangle bounds)
		{
			Insist.IsNotNull(CollisionLayer, "collisionLayer must not be null!");

			// offset the passed in world position to compensate for the entity position
			bounds.Location -= (Entity.Transform.Position + _localOffset).ToPoint();
			return CollisionLayer.GetTilesIntersectingBounds(bounds);
		}

		#endregion


		#region Component overrides

		public override void OnEntityTransformChanged(Transform.Component comp)
		{
			// we only deal with positional changes here. TiledMaps cant be scaled.
			if (_shouldCreateColliders && comp == Transform.Component.Position)
			{
				RemoveColliders();
				AddColliders();
			}
		}

		public override void OnAddedToEntity() => AddColliders();

		public override void OnRemovedFromEntity() => RemoveColliders();

		public virtual void Update()
		{
			if (AutoUpdateTilesets)
				TiledMap.Update();
		}

		public override void Render(Batcher batcher, Camera camera)
		{
			if (LayerIndicesToRender == null)
			{
				TiledRendering.RenderMap(TiledMap, batcher, Entity.Transform.Position + _localOffset, Transform.Scale, LayerDepth, camera.Bounds);
			}
			else
			{
				for (var i = 0; i < TiledMap.Layers.Count; i++)
				{
					if (TiledMap.Layers[i].Visible && LayerIndicesToRender.Contains(i))
						TiledRendering.RenderLayer(TiledMap.Layers[i], batcher, Entity.Transform.Position + _localOffset, Transform.Scale, LayerDepth, camera.Bounds);
				}
			}
		}

		public override void DebugRender(Batcher batcher)
		{
			foreach (var group in TiledMap.ObjectGroups)
				TiledRendering.RenderObjectGroup(group, batcher, Entity.Transform.Position + _localOffset, Transform.Scale, LayerDepth);

			if (_colliders != null)
			{
				foreach (var collider in _colliders)
					collider.DebugRender(batcher);
			}
		}

		#endregion


		#region Colliders

		/// <summary>
		/// Adds the colliders for the given layer name. If physicsLayer is -1, it will use the default PhysicsLayer set on this component. This method can be used to add colliders for multiple layers if you have more than one collision layer in your TiledMap.
		/// </summary>
		/// <param name="layerName"></param>
		/// <param name="physicsLayer"></param>
		/// <returns></returns>
		public Collider[] AddCollidersForLayer(string layerName, int physicsLayer = -1)
		{
			if(physicsLayer == -1)
				physicsLayer = PhysicsLayer;
			// fetch the collision layer and its rects for collision
			var layer = TiledMap.TileLayers[layerName];
			if(layer == null)
			{
				Debug.Error("TiledMapRenderer: No layer found with name {0}", layerName);
				return new Collider[0];
			}
			var collisionRects = layer.GetCollisionRectangles();

			// create colliders for the rects we received
			var colliders = new Collider[collisionRects.Count];
			for (var i = 0; i < collisionRects.Count; i++)
			{
				var collider = new BoxCollider(collisionRects[i].X + _localOffset.X,
					collisionRects[i].Y + _localOffset.Y, collisionRects[i].Width, collisionRects[i].Height);
				collider.PhysicsLayer = physicsLayer;
				collider.Entity = Entity;
				colliders[i] = collider;

				Physics.AddCollider(collider);
			}

			if(_colliders is null)
				_colliders = colliders;
			else
			{
				var length = _colliders.Length + colliders.Length;
				var newColliders = new Collider[length];
				_colliders.CopyTo(newColliders, 0);
				colliders.CopyTo(newColliders, _colliders.Length);
				_colliders = newColliders;
			}
				
			return colliders;
		}

		public void AddColliders()
		{
			if (CollisionLayer == null || !_shouldCreateColliders)
				return;

			AddCollidersForLayer(CollisionLayer.Name);
		}

		public void RemoveColliders()
		{
			if (_colliders == null)
				return;

			foreach (var collider in _colliders)
				Physics.RemoveCollider(collider);
			_colliders = null;
		}

		#endregion
	}
}
