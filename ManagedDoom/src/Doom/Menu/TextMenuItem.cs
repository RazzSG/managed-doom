namespace ManagedDoom;

public sealed class TextMenuItem(string name, int skullX, int skullY, int itemX, int itemY, MenuDef next) : MenuItem(skullX, skullY, next)
{
    public string Name => name;
    public int ItemX => itemX;
    public int ItemY => itemY;
}