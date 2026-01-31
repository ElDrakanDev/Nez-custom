using System;
using System.Collections.Generic;
using System.Linq;
using Nez.Persistence;
using Nez.Textures;


namespace Nez.Sprites
{
	/// <summary>
	/// SpriteAnimator handles the display and animation of a sprite
	/// </summary>
	[System.Serializable]
	public class SpriteAnimator : SpriteRenderer, IUpdatable
	{
		public enum LoopMode
		{
			/// <summary>
			/// Play the sequence in a loop forever [A][B][C][A][B][C][A][B][C]...
			/// </summary>
			Loop,

			/// <summary>
			/// Play the sequence once [A][B][C] then pause and set time to 0 [A]
			/// </summary>
			Once,

			/// <summary>
			/// Plays back the animation once, [A][B][C]. When it reaches the end, it will keep playing the last frame and never stop playing
			/// </summary>
			ClampForever,

			/// <summary>
			/// Play the sequence in a ping pong loop forever [A][B][C][B][A][B][C][B]...
			/// </summary>
			PingPong,

			/// <summary>
			/// Play the sequence once forward then back to the start [A][B][C][B][A] then pause and set time to 0
			/// </summary>
			PingPongOnce
		}

		public enum State
		{
			None,
			Running,
			Paused,
			Completed
		}

		/// <summary>
		/// fired when an animation completes, includes the animation name;
		/// </summary>
		public event Action<string> OnAnimationCompletedEvent;

		/// <summary>
		/// animation playback speed
		/// </summary>
		public float Speed = 1;

		/// <summary>
		/// the current state of the animation
		/// </summary>
		[NsonExclude] public State AnimationState { get; private set; } = State.None;

		/// <summary>
		/// the current animation
		/// </summary>
		[NsonExclude] public SpriteAnimation CurrentAnimation { get; private set; }

		/// <summary>
		/// the name of the current animation
		/// </summary>
		public string CurrentAnimationName { get; private set; }

		/// <summary>
		/// index of the current frame in sprite array of the current animation
		/// </summary>
		[NsonExclude] public int CurrentFrame { get; set; }

		/// <summary>
		/// checks to see if the CurrentAnimation is running
		/// </summary>
		public bool IsRunning => AnimationState == State.Running;

		/// <summary>
		/// Provides access to list of available animations
		/// </summary>
		[NsonExclude] public Dictionary<string, SpriteAnimation> Animations { get { return _animations; } }

		[NsonExclude] Dictionary<string, SpriteAnimation> _animations = new Dictionary<string, SpriteAnimation>();

		/// <summary>
		/// The path of the atlas file. Automatically calls AddAnimationsFromAtlas if set when added to Entity
		/// </summary>
		public string AtlasPath;

		float _elapsedTime;
		LoopMode _loopMode;


		public SpriteAnimator()
		{ }

		public SpriteAnimator(Sprite sprite) => SetSprite(sprite);

		public override void OnAddedToEntity()
		{
			if(!string.IsNullOrEmpty(AtlasPath) && Animations.Count == 0)
				AddAnimationsFromAtlas(Entity.Scene.Content.LoadSpriteAtlas(AtlasPath));
			if(Animations.Count == 1)
				Play(Animations.Keys.First());
		}

		public virtual void Update()
		{
			if (AnimationState != State.Running || CurrentAnimation == null)
				return;

			var animation = CurrentAnimation;
			var secondsPerFrame = 1 / (animation.FrameRates[CurrentFrame] * Speed);
			var iterationDuration = secondsPerFrame * animation.Sprites.Length;
			var pingPongIterationDuration = animation.Sprites.Length < 3 ? iterationDuration : secondsPerFrame * (animation.Sprites.Length * 2 - 2);

			_elapsedTime += Time.DeltaTime;
			var time = Math.Abs(_elapsedTime);

			// Once and PingPongOnce reset back to Time = 0 once they complete
			if (_loopMode == LoopMode.Once && time > iterationDuration ||
				_loopMode == LoopMode.PingPongOnce && time > pingPongIterationDuration)
			{
				AnimationState = State.Completed;
				_elapsedTime = 0;
				CurrentFrame = 0;
				Sprite = animation.Sprites[0];
				OnAnimationCompletedEvent?.Invoke(CurrentAnimationName);
				return;
			}

			if (_loopMode == LoopMode.ClampForever && time > iterationDuration)
			{
				AnimationState = State.Completed;
				CurrentFrame = animation.Sprites.Length - 1;
				Sprite = animation.Sprites[CurrentFrame];
				OnAnimationCompletedEvent?.Invoke(CurrentAnimationName);
				return;
			}

			// figure out which frame we are on
			int i = Mathf.FloorToInt(time / secondsPerFrame);
			int n = animation.Sprites.Length;
			if (n > 2 && (_loopMode == LoopMode.PingPong || _loopMode == LoopMode.PingPongOnce))
			{
				// create a pingpong frame
				int maxIndex = n - 1;
				CurrentFrame = maxIndex - Math.Abs(maxIndex - i % (maxIndex * 2));
			}
			else
				// create a looping frame
				CurrentFrame = i % n;

			Sprite = animation.Sprites[CurrentFrame];
		}

		/// <summary>
		/// adds all the animations from the SpriteAtlas
		/// </summary>
		public SpriteAnimator AddAnimationsFromAtlas(SpriteAtlas atlas)
		{
			AtlasPath = atlas.AtlasPath;

			for (var i = 0; i < atlas.AnimationNames.Length; i++)
			{
				if (_animations.ContainsKey(atlas.AnimationNames[i]))
				{
					Debug.Warn($"ATLAS '{AtlasPath}': Tried to add animation {atlas.AnimationNames[i]} but it already exists.");
					continue;
				}
				_animations.Add(atlas.AnimationNames[i], atlas.SpriteAnimations[i]);
			}
			return this;
		}

		/// <summary>
		/// Adds a SpriteAnimation
		/// </summary>
		public SpriteAnimator AddAnimation(string name, SpriteAnimation animation)
		{
			// if we have no sprite use the first frame we find
			if (Sprite == null && animation.Sprites.Length > 0)
				SetSprite(animation.Sprites[0]);
			_animations[name] = animation;
			return this;
		}

		public SpriteAnimator AddAnimation(string name, Sprite[] sprites, float fps = 10) => AddAnimation(name, fps, sprites);

		public SpriteAnimator AddAnimation(string name, float fps, params Sprite[] sprites)
		{
			AddAnimation(name, new SpriteAnimation(sprites, fps));
			return this;
		}

		public override Component Clone()
		{
			var anims = _animations;
			_animations = new Dictionary<string, SpriteAnimation>(_animations);
			var component = base.Clone();
			_animations = anims;
			return component;
		}

		public override void Render(Batcher batcher, Camera camera)
		{
			if(AnimationState == State.Completed && _loopMode == LoopMode.Once)
				return;
			base.Render(batcher, camera);
		}

		#region Playback

		/// <summary>
		/// plays the animation with the given name. If no loopMode is specified it is defaults to Loop
		/// </summary>
		public void Play(string name, LoopMode? loopMode = null)
		{
			CurrentAnimation = _animations[name];
			CurrentAnimationName = name;
			CurrentFrame = 0;
			AnimationState = State.Running;

			Sprite = CurrentAnimation.Sprites[0];
			_elapsedTime = 0;
			_loopMode = loopMode ?? LoopMode.Loop;
		}

		/// <summary>
		/// checks to see if the animation is playing (i.e. the animation is active. it may still be in the paused state)
		/// </summary>
		public bool IsAnimationActive(string name) => CurrentAnimation != null && CurrentAnimationName.Equals(name);

		/// <summary>
		/// pauses the animator
		/// </summary>
		public void Pause() => AnimationState = State.Paused;

		/// <summary>
		/// unpauses the animator
		/// </summary>
		public void UnPause() => AnimationState = State.Running;

		/// <summary>
		/// stops the current animation and nulls it out
		/// </summary>
		public void Stop()
		{
			CurrentAnimation = null;
			CurrentAnimationName = null;
			CurrentFrame = 0;
			AnimationState = State.None;
		}

		#endregion

		#region Inspector methods
		[InspectorCallable]
		public void NextFrame()
		{
			if(CurrentAnimation != null)
			{
				CurrentFrame = (CurrentFrame + 1) % CurrentAnimation.Sprites.Length;
				Sprite = CurrentAnimation.Sprites[CurrentFrame];
			}
		}

		#endregion
	}
}