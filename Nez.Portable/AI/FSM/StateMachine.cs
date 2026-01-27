using System;
using System.Collections.Generic;


namespace Nez.AI.FSM
{
	public class StateMachine<T>
	{
		public event Action OnStateChanged;

		public State<T> CurrentState => _currentState;

		public State<T> PreviousState;
		public float ElapsedTimeInState = 0f;

		protected State<T> _currentState;
		protected T _context;
		readonly List<State<T>> _states = new List<State<T>>();


		public StateMachine(T context, State<T> initialState)
		{
			_context = context;

			// setup our initial state
			AddState(initialState);
			_currentState = initialState;
			_currentState.Begin();
		}


		/// <summary>
		/// adds the state to the machine
		/// </summary>
		public void AddState(State<T> state)
		{
			state.SetMachineAndContext(this, _context);
			_states.Add(state);
		}


		/// <summary>
		/// ticks the state machine with the provided delta time
		/// </summary>
		public virtual void Update(float deltaTime)
		{
			ElapsedTimeInState += deltaTime;
			_currentState.Reason();
			_currentState.Update(deltaTime);
		}

		/// <summary>
		/// Gets a specific state from the machine without having to
		/// change to it.
		/// </summary>
		public virtual R GetState<R>() where R : State<T>
		{
			for (var i = 0; i < _states.Count; i++)
			{
				if (_states[i] is R typedState)
					return typedState;
			}

			var type = typeof(R);
			Insist.IsTrue(false,
				"{0}: state {1} does not exist. Did you forget to add it by calling addState?", GetType(), type);
			return null;
		}

		/// <summary>
		/// changes the current state
		/// </summary>
		public R ChangeState<R>() where R : State<T>
		{
			var newState = GetState<R>();
			return ChangeState(newState);
		}

		/// <summary>
		/// changes the current state to the provided instance
		/// </summary>
		public R ChangeState<R>(R state) where R : State<T>
		{
			Insist.IsNotNull(state);
			Insist.IsTrue(_states.Contains(state),
				"{0}: state instance {1} is not registered. Did you forget to add it by calling addState?", GetType(), state.GetType());

			// avoid changing to the same state
			if (_currentState != null && _currentState.GetType() == state.GetType())
				return _currentState as R;

			// only call end if we have a currentState
			if (_currentState != null)
				_currentState.End();

			// swap states and call begin
			ElapsedTimeInState = 0f;
			PreviousState = _currentState;
			_currentState = state;
			_currentState.Begin();

			// fire the changed event if we have a listener
			if (OnStateChanged != null)
				OnStateChanged();

			return _currentState as R;
		}
	}
}