namespace Echo;

using System;
using System.Collections.Generic;
using System.Threading;

public interface IState<TEvent>
{
    string Name { get; }
    Task UpdateAsync(TEvent e, CancellationToken cancellationToken);
}

public sealed class StateMachine<TEvent> : IState<TEvent>
{
    private State<TEvent>? currentState;

    public StateMachine(State<TEvent> initialState)
    {
        this.currentState = initialState;
    }

    public string Name { get; init; } = "StateMachine";

    public async Task UpdateAsync(TEvent e, CancellationToken cancellationToken)
    {
        var entry = this.currentState?.GetFinalEntryState();
        if (entry != null)
        {
            Transit(entry);
        }

        if (this.currentState is not null)
        {
            await this.currentState.UpdateAsync(e, cancellationToken);
        }

        var to = this.currentState?.GetTransitionState(e);
        if (to != null)
        {
            Transit(to);
        }
    }

    private void Transit(State<TEvent> state)
    {
        this.currentState?.Exit();
        this.currentState = state;
        this.currentState.Enter();
    }
}

public abstract class State<TEvent> : IState<TEvent>
{
    private readonly List<State<TEvent>> parents = [];
    private readonly Dictionary<State<TEvent>, Func<TEvent, bool>[]> transitions = [];

    public string Name { get; init; } = "State";

    public virtual void Enter() { }
    public abstract Task UpdateAsync(TEvent e, CancellationToken cancellationToken);
    public virtual void Exit() { }
    public virtual bool CanExit(TEvent e) => true;

    public State<TEvent> To(State<TEvent> to)
    {
        return To(to, CanExit);
    }

    public State<TEvent> To(State<TEvent> to, params Func<TEvent, bool>[] conditions)
    {
        this.transitions.Add(to, conditions);
        return to;
    }

    public virtual State<TEvent>? GetFinalEntryState() => default;

    public State<TEvent>? GetTransitionState(TEvent e)
    {
        foreach (var parent in this.parents)
        {
            var to = parent.GetTransitionState(e);
            if (to != null)
            {
                return to;
            }
        }

        foreach (var (state, conditions) in this.transitions)
        {
            var transition = true;
            foreach (var condition in conditions)
            {
                if (!condition(e))
                {
                    transition = false;
                    break;
                }
            }

            if (transition)
            {
                return state;
            }
        }

        return null;
    }


    protected internal void AddParent(State<TEvent> parent)
    {
        this.parents.Add(parent);
    }
}

public sealed class StateGroup<TEvent> : State<TEvent>
{
    private readonly State<TEvent> entryState;

    public StateGroup(params State<TEvent>[] states)
    {
        this.entryState = states[0];
        foreach (var state in states)
        {
            state.AddParent(this);
        }
    }

    public override Task UpdateAsync(TEvent e, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override State<TEvent>? GetFinalEntryState() => this.entryState.GetFinalEntryState() ?? this.entryState;
}

public sealed class DelegateState<TEvent> : State<TEvent>
{
    private readonly Action? onEnter;
    private readonly Func<TEvent, CancellationToken, Task> onUpdate;
    private readonly Action? onExit;

    public DelegateState(Func<TEvent, CancellationToken, Task> onUpdate) : this(onUpdate.Method.Name, onUpdate)
    {
    }

    public DelegateState(Action onEnter, Func<TEvent, CancellationToken, Task> onUpdate) : this(onUpdate)
    {
        this.onEnter = onEnter;
    }

    public DelegateState(Action onEnter, Func<TEvent, CancellationToken, Task> onUpdate, Action onExit) : this(onEnter, onUpdate)
    {
        this.onExit = onExit;
    }

    public DelegateState(string name, Func<TEvent, CancellationToken, Task> onUpdate)
    {
        this.Name = name;
        this.onUpdate = onUpdate;
    }

    public DelegateState(string name, Action onEnter, Func<TEvent, CancellationToken, Task> onUpdate) : this(name, onUpdate)
    {
        this.onEnter = onEnter;
    }

    public DelegateState(string name, Action onEnter, Func<TEvent, CancellationToken, Task> onUpdate, Action onExit) : this(name, onEnter, onUpdate)
    {
        this.onExit = onExit;
    }

    public override void Enter() => this.onEnter?.Invoke();

    public override Task UpdateAsync(TEvent e, CancellationToken cancellationToken) => this.onUpdate(e, cancellationToken);

    public override void Exit() => this.onExit?.Invoke();
}
