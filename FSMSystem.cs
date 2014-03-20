using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
A Finite State Machine System based on Chapter 3.1 of Game Programming Gems 1 by Eric Dybsand
 
Written by Roberto Cezar Bianchini, July 2010
 
Modified by Ed Key, October 2013
 
Modified by Jonathan Brodsky, Jan 2014

How to use:
	1. Place the labels for the transitions and the states of the Finite State System
		in the corresponding enums.
 
	2. Write new class(es) inheriting from FSMState and fill each one with pairs (transition-state).
		These pairs represent the state S2 the FSMSystem should be if while being on state S1, a
		transition T is fired and state S1 has a transition from it to S2. Remember this is a Deterministic FSM. 
		You can't have one transition leading to two different states.
 
	   Method Reason is used to determine which transition should be fired.
	   You can write the code to fire transitions in another place, and leave this method empty if you
	   feel it's more appropriate to your project.
 
	   Method Act has the code to perform the actions the NPC is supposed do if it's on this state.
	   You can write the code for the actions in another place, and leave this method empty if you
	   feel it's more appropriate to your project.
 
	3. Create an instance of FSMSystem class and add the states to it.
 
	4. Call Reason and Act (or whichever methods you have for firing transitions and making the NPCs
		 behave in your game) from your Update or FixedUpdate methods.
 
	Asynchronous transitions from Unity Engine, like OnTriggerEnter, SendMessage, can also be used, 
	just call the Method PerformTransition from your FSMSystem instance with the correct Transition 
	when the event occurs.
 
 
 
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE 
AND NON-INFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/


/// <summary>
/// Place the labels for the Transitions in this enum.
/// Don't change the first label, NullTransition as FSMSystem class uses it.
/// </summary>
public enum FsmTransitionId
{
	NullTransition = 0, // Use this transition to represent a non-existing transition in your system

	// play transitions
	Complete, // a default transition state
	GameOver,
	Rebuild,
}

/// <summary>
/// Place the labels for the States in this enum.
/// Don't change the first label, NullTransition as FSMSystem class uses it.
/// </summary>
public enum FsmStateId
{
	NullStateID = 0, // Use this ID to represent a non-existing State in your system	

	// play states
	Player,
	InitialGen,
	Monster,
	Bullets,
	GameOver,
	World,
}

#region CODE
/// <summary>
/// This class represents the States in the Finite State System.
/// Each state has a Dictionary with pairs (transition-state) showing
/// which state the FSM should be if a transition is fired while this state
/// is the current state.
/// Method Reason is used to determine which transition should be fired .
/// Method Act has the code to perform the actions the NPC is supposed do if it's on this state.
/// </summary>
public class FsmState
{
	protected Dictionary<FsmTransitionId, FsmStateId> transitionMap = new Dictionary<FsmTransitionId, FsmStateId>();
	protected FsmStateId stateID;
	public FsmStateId ID { get { return stateID; } }

	protected FsmSystem machine;
	public void SetOwner(FsmSystem _fsm)
	{
		machine = _fsm;
	}

	public FsmState(FsmStateId _stateId)
	{
		stateID = _stateId;
	}

	// fluid interface
	public FsmState WithTransition(FsmTransitionId trans, FsmStateId id)
	{
		AddTransition(trans, id);
		return this;
	}
	
	// regular add method
	public void AddTransition(FsmTransitionId trans, FsmStateId id)
	{
		// Check if anyone of the args is invalid
		if (trans == FsmTransitionId.NullTransition)
		{
			throw new Exception("FSMState ERROR: NullTransition is not allowed for a real transition");
		}

		if (id == FsmStateId.NullStateID)
		{
			throw new Exception("FSMState ERROR: NullStateID is not allowed for a real ID");
		}

		// Since this is a Deterministic FSM,
		//   check if the current transition was already inside the map
		if (transitionMap.ContainsKey(trans))
		{
			throw new Exception("FSMState ERROR: State " + stateID.ToString() + " already has transition " + trans.ToString() +
						   "Impossible to assign to another state");
		}

		transitionMap.Add(trans, id);
	}

	/// <summary>
	/// This method deletes a pair transition-state from this state's map.
	/// If the transition was not inside the state's map, an ERROR message is printed.
	/// </summary>
	public void DeleteTransition(FsmTransitionId trans)
	{
		// Check for NullTransition
		if (trans == FsmTransitionId.NullTransition)
		{
			throw new Exception("FSMState ERROR: NullTransition is not allowed");
		}

		// Check if the pair is inside the map before deleting
		if (transitionMap.ContainsKey(trans))
		{
			transitionMap.Remove(trans);
			return;
		}
		throw new Exception("FSMState ERROR: Transition " + trans.ToString() + " passed to " + stateID.ToString() +
					   " was not on the state's transition list");
	}

	/// <summary>
	/// This method returns the new state the FSM should be if
	///    this state receives a transition and 
	/// </summary>
	public FsmStateId GetOutputState(FsmTransitionId trans)
	{
		// Check if the map has this transition
		if (transitionMap.ContainsKey(trans))
		{
			return transitionMap[trans];
		}
		return FsmStateId.NullStateID;
	}

	// helper for the "Reason" method
	public void PerformTransition(FsmTransitionId trans)
	{
		Debug.Log ("going to" + trans.ToString());
		machine.PerformTransition(trans);
	}

	Action beforeEnteringAction;
	Action beforeLeavingAction;
	Action updateAction;
	Action onGuiAction;

	public FsmState WithBeforeEnteringAction(Action action)
	{
		beforeEnteringAction = action;
		return this;
	}

	public FsmState WithBeforeLeavingAction(Action action)
	{
		beforeLeavingAction = action;
		return this;
	}

	public FsmState WithUpdateAction(Action action)
	{
		updateAction = action;
		return this;
	}

	public FsmState WithOnGuiAction(Action action)
	{
		onGuiAction = action;
		return this;
	}
	

	/// <summary>
	/// This method is used to set up the State condition before entering it.
	/// It is called automatically by the FSMSystem class before assigning it
	/// to the current state.
	/// </summary>
	public virtual void DoBeforeEntering() 
	{
		if (beforeEnteringAction != null)
		{
			beforeEnteringAction();
		}
	}

	/// <summary>
	/// This method is used to make anything necessary, as reseting variables
	/// before the FSMSystem changes to another one. It is called automatically
	/// by the FSMSystem before changing to a new state.
	/// </summary>
	public virtual void DoBeforeLeaving()
	{
		if (beforeLeavingAction != null)
		{
			beforeLeavingAction();
		}
	}

	/// <summary>
	/// This method is a generic update - just do stuff that this state does per frame
	/// </summary>
	public virtual void Update()
	{
		if (updateAction != null)
		{
			updateAction();
		}
	}

	/// Called by Unity's OnGUI for buttons and stuff
	public virtual void OnGui()
	{
		if (onGuiAction != null)
		{
			onGuiAction();
		}
	}

} // class FSMState


/// <summary>
/// FSMSystem class represents the Finite State Machine class.
///  It has a List with the States the NPC has and methods to add,
///  delete a state, and to change the current state the Machine is on.
/// </summary>
public class FsmSystem
{
	private List<FsmState> states;

	// The only way one can change the state of the FSM is by performing a transition
	// Don't change the CurrentState directly
	private FsmStateId currentStateID;
	public FsmStateId CurrentStateID { get { return currentStateID; } }
	private FsmState currentState;
	public FsmState CurrentState { get { return currentState; } }

	public FsmSystem()
	{
		states = new List<FsmState>();
	}

	/// <summary>
	/// This method places new states inside the FSM,
	/// or prints an ERROR message if the state was already inside the List.
	/// First state added is also the initial state.
	/// </summary>
	public void AddState(FsmState s)
	{
		// Check for Null reference before deleting
		if (s == null)
		{
			throw new Exception("FSM ERROR: Null reference is not allowed");
		}

		s.SetOwner(this);

		// First State inserted is also the Initial state,
		//   the state the machine is in when the simulation begins
		if (states.Count == 0)
		{
			states.Add(s);
			currentState = s;
			currentStateID = s.ID;
			return;
		}

		// Add the state to the List if it's not inside it
		foreach (FsmState state in states)
		{
			if (state.ID == s.ID)
			{
				throw new Exception("FSM ERROR: Impossible to add state " + s.ID.ToString() +
							   " because state has already been added");
			}
		}

		states.Add(s);
	}

	/// <summary>
	/// Call this to trigger the before action on the first state in the list
	/// </summary>
	public void Start(){
		currentState.DoBeforeEntering ();
	}

	/// <summary>
	/// This method delete a state from the FSM List if it exists, 
	///   or prints an ERROR message if the state was not on the List.
	/// </summary>
	public void DeleteState(FsmStateId id)
	{
		// Check for NullState before deleting
		if (id == FsmStateId.NullStateID)
		{
			throw new Exception("FSM ERROR: NullStateID is not allowed for a real state");
		}

		// Search the List and delete the state if it's inside it
		foreach (FsmState state in states)
		{
			if (state.ID == id)
			{
				states.Remove(state);
				return;
			}
		}

		throw new Exception("FSM ERROR: Impossible to delete state " + id.ToString() +
					   ". It was not on the list of states");
	}

	/// <summary>
	/// This method tries to change the state the FSM is in based on
	/// the current state and the transition passed. If current state
	///  doesn't have a target state for the transition passed, 
	/// an ERROR message is printed.
	/// </summary>
	public void PerformTransition(FsmTransitionId trans)
	{
		// Check for NullTransition before changing the current state
		if (trans == FsmTransitionId.NullTransition)
		{
			throw new Exception("FSM ERROR: NullTransition is not allowed for a real transition");
		}

		// Check if the currentState has the transition passed as argument
		FsmStateId id = currentState.GetOutputState(trans);
		if (id == FsmStateId.NullStateID)
		{
			throw new Exception("FSM ERROR: State " + currentStateID.ToString() + " does not have a target state " +
						   " for transition " + trans.ToString());
		}

		// Update the currentStateID and currentState		
		currentStateID = id;
		foreach (FsmState state in states)
		{
			if (state.ID == currentStateID)
			{
				// Do the post processing of the state before setting the new one
				currentState.DoBeforeLeaving();

				currentState = state;

				// Reset the state to its desired condition before it can reason or act
				currentState.DoBeforeEntering();
				break;
			}
		}

	} // PerformTransition()

} //class FSMSystem

#endregion