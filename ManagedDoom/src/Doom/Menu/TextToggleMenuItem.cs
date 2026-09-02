using System;

namespace ManagedDoom;

public sealed class TextToggleMenuItem(string name, int skullX, int skullY, int itemX, int itemY, string state1, string state2, int stateX, Func<int> reset, Action<int> action) : MenuItem(skullX, skullY, null)
{
    private readonly string[] states =
    [
        state1,
        state2
    ];

    private int stateNumber;

    public void Reset()
    {
        stateNumber = reset?.Invoke() ?? 0;

        stateNumber = Math.Clamp(stateNumber, 0, states.Length - 1);
    }

    public void Up()
    {
        stateNumber++;

        if (stateNumber >= states.Length)
        {
            stateNumber = 0;
        }

        action?.Invoke(stateNumber);
    }

    public void Down()
    {
        stateNumber--;

        if (stateNumber < 0)
        {
            stateNumber = states.Length - 1;
        }

        action?.Invoke(stateNumber);
    }

    public string Name => name;
    public int ItemX => itemX;
    public int ItemY => itemY;

    public string State => states[stateNumber];
    public int StateX => stateX;
}