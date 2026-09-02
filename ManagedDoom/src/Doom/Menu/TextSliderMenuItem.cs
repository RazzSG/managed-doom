using System;

namespace ManagedDoom;

public sealed class TextSliderMenuItem(string name, int skullX, int skullY, int itemX, int itemY, int sliderLength, Func<int> reset, Action<int> action) : MenuItem(skullX, skullY, null)
{
    private int sliderPosition = 0;

    public void Reset()
    {
        sliderPosition = reset?.Invoke() ?? 0;
        sliderPosition = Math.Clamp(sliderPosition, 0, sliderLength - 1);
    }

    public void Up()
    {
        if (sliderPosition < sliderLength - 1)
        {
            sliderPosition++;
            action?.Invoke(sliderPosition);
        }
    }

    public void Down()
    {
        if (sliderPosition > 0)
        {
            sliderPosition--;
            action?.Invoke(sliderPosition);
        }
    }

    public string Name => name;
    public int ItemX => itemX;
    public int ItemY => itemY;

    public int SliderLength => sliderLength;
    public int SliderPosition => sliderPosition;
}