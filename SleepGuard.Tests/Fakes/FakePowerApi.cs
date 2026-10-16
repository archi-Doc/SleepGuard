using SleepGuard;

namespace SleepGuard.Tests;

internal sealed class FakePowerApi : IPowerApi
{
    private int valueWrites;

    internal PowerSnapshot State { get; private set; } = new(Guid.NewGuid(), new(600, 1200, 2, 3), Guid.NewGuid(), Guid.NewGuid());

    internal int FailAtMutation
    {
        get;
        init;
    }

    internal bool IgnoreFirstValues
    {
        get;
        init;
    }

    internal bool FailRead
    {
        get;
        init;
    }

    internal bool FailRestoreValues
    {
        get;
        init;
    }

    internal int Mutations
    {
        get;
        private set;
    }

    public PowerSnapshot Read() => FailRead ? throw new Exception("Injected read failure.") : State;

    public void WriteValues(PowerValues values)
    {
        valueWrites++;
        if (IgnoreFirstValues && valueWrites == 1)
        {
            Mutations++;
            return;
        }

        if (FailRestoreValues && valueWrites > 1)
        {
            Mutations++;
            throw new Exception("Injected restore failure.");
        }

        // Simulate a native failure after a partial write.
        State = State with
        {
            Values = State.Values with
            {
                BatterySleep = values.BatterySleep,
            },
        };
        Mutate();
        State = State with
        {
            Values = values,
        };
    }

    public void ActivateScheme(Guid scheme)
    {
        Mutate();
        State = State with
        {
            ActiveScheme = scheme,
        };
    }

    public void SetBatteryMode(Guid mode)
    {
        Mutate();
        State = State with
        {
            BatteryMode = mode,
        };
    }

    public void SetPluggedInMode(Guid mode)
    {
        Mutate();
        State = State with
        {
            PluggedInMode = mode,
        };
    }

    private void Mutate()
    {
        if (++Mutations == FailAtMutation)
        {
            throw new Exception("Injected native failure.");
        }
    }
}
