using SleepGuard;
using SleepGuard.Tests;

var passed = 0;
Test("Normal uses five-minute sleep and sleep on lid close", () =>
{
    var api = new FakePowerApi();
    new PowerManager(api).Apply(PowerProfile.Normal);
    Equal(new PowerSnapshot(PowerManager.BalancedScheme, new(300, 300, 1, 1), Guid.Empty, Guid.Empty), api.State);
});

Test("Awake uses five minutes on battery and five hours on AC", () =>
{
    var api = new FakePowerApi();
    new PowerManager(api).Apply(PowerProfile.Awake);
    Equal(new PowerSnapshot(PowerManager.BalancedScheme, new(300, 18000, 0, 0), Guid.Empty, Guid.Empty), api.State);
});

Test("Normal restores the required settings after Awake", () =>
{
    var api = new FakePowerApi();
    var manager = new PowerManager(api);
    manager.Apply(PowerProfile.Awake);
    manager.Apply(PowerProfile.Normal);
    Equal(new PowerValues(300, 300, 1, 1), api.State.Values);
});
foreach (var step in Enumerable.Range(1, 4))
{
    Test($"A failure at mutation {step} rolls back the previous state", () =>
    {
        var api = new FakePowerApi { FailAtMutation = step };
        var original = api.State;
        var message = Failure(() => new PowerManager(api).Apply(PowerProfile.Awake));
        Equal(original, api.State);
        Require(message.Contains("previous settings were restored"));
    });
}

Test("A read-back mismatch is rejected and rolled back", () =>
{
    var api = new FakePowerApi { IgnoreFirstValues = true };
    var original = api.State;
    var message = Failure(() => new PowerManager(api).Apply(PowerProfile.Awake));
    Require(message.Contains("did not retain"));
    Equal(original, api.State);
});

Test("A snapshot failure does not change settings", () =>
{
    var api = new FakePowerApi { FailRead = true };
    Failure(() => new PowerManager(api).Apply(PowerProfile.Awake));
    Equal(0, api.Mutations);
});

Test("Rollback continues after a restore failure", () =>
{
    var api = new FakePowerApi { FailAtMutation = 3, FailRestoreValues = true };
    var original = api.State;
    var message = Failure(() => new PowerManager(api).Apply(PowerProfile.Awake));
    Require(message.Contains("could not be restored"));
    Equal(original.ActiveScheme, api.State.ActiveScheme);
    Equal(original.BatteryMode, api.State.BatteryMode);
    Equal(original.PluggedInMode, api.State.PluggedInMode);
    Equal(7, api.Mutations);
});

Test("Invalid profiles do not change settings", () =>
{
    var api = new FakePowerApi();
    Failure(() => new PowerManager(api).Apply((PowerProfile)99));
    Equal(0, api.Mutations);
});

// This optional check reads real Windows settings but never changes them.
if (args.Contains("--read-windows"))
{
    Test("Windows native APIs can read the current configuration", () =>
    {
        Console.WriteLine(new WindowsPowerApi().Read());
    });
}

Console.WriteLine($"Passed {passed} tests. No real power settings were changed.");

void Test(string name, Action action)
{
    action();
    passed++;
    Console.WriteLine($"PASS: {name}");
}

static void Equal<T>(T expected, T actual) where T : IEquatable<T>
{
    if (!expected.Equals(actual))
    {
        throw new Exception($"Expected {expected}; actual {actual}.");
    }
}

static void Require(bool value)
{
    if (!value)
    {
        throw new Exception("Assertion failed.");
    }
}

static string Failure(Action action)
{
    try
    {
        action();
    }
    catch (Exception error)
    {
        return error.Message;
    }

    throw new Exception("Expected an exception.");
}
