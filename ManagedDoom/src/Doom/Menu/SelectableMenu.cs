//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//



using System;
using System.Collections.Generic;

namespace ManagedDoom
{
    public sealed class SelectableMenu : MenuDef
    {
        private string[] name;
        private int[] titleX;
        private int[] titleY;
        private MenuItem[] items;
        
        private MenuDef parent;

        private int index;
        private MenuItem choice;

        private TextInput textInput;

        public SelectableMenu(
            DoomMenu menu,
            string name, int titleX, int titleY,
            int firstChoice,
            params MenuItem[] items) : base(menu)
        {
            this.name = new[] { name };
            this.titleX = new[] { titleX };
            this.titleY = new[] { titleY };
            this.items = items;

            index = firstChoice;
            choice = items[index];
        }

        public SelectableMenu(
            DoomMenu menu,
            string name1, int titleX1, int titleY1,
            string name2, int titleX2, int titleY2,
            int firstChoice,
            params MenuItem[] items) : base(menu)
        {
            this.name = new[] { name1, name2 };
            this.titleX = new[] { titleX1, titleX2 };
            this.titleY = new[] { titleY1, titleY2 };
            this.items = items;

            index = firstChoice;
            choice = items[index];
        }

        public override void Open()
        {
            foreach (var item in items)
            {
                var toggle = item as ToggleMenuItem;
                if (toggle != null)
                {
                    toggle.Reset();
                }

                var textToggle = item as TextToggleMenuItem;
                if (textToggle != null)
                {
                    textToggle.Reset();
                }

                var slider = item as SliderMenuItem;
                if (slider != null)
                {
                    slider.Reset();
                }

                var textSlider = item as TextSliderMenuItem;
                if (textSlider != null)
                {
                    textSlider.Reset();
                }

                var choice = item as ChoiceMenuItem;
                if (choice != null)
                {
                    choice.Reset();
                }
            }
        }

        private void Up()
        {
            index--;
            if (index < 0)
            {
                index = items.Length - 1;
            }

            choice = items[index];
        }

        private void Down()
        {
            index++;
            if (index >= items.Length)
            {
                index = 0;
            }

            choice = items[index];
        }

        public override bool DoEvent(DoomEvent e)
        {
            if (e.Type != EventType.KeyDown)
            {
                return true;
            }

            if (textInput != null)
            {
                var result = textInput.DoEvent(e);

                if (textInput.State == TextInputState.Canceled)
                {
                    textInput = null;
                }
                else if (textInput.State == TextInputState.Finished)
                {
                    textInput = null;
                }

                if (result)
                {
                    return true;
                }
            }

            if (e.Key == DoomKey.Up)
            {
                Up();
                Menu.StartSound(Sfx.PSTOP);
                return true;
            }

            if (e.Key == DoomKey.Down)
            {
                Down();
                Menu.StartSound(Sfx.PSTOP);
                return true;
            }

            if (e.Key == DoomKey.Left)
            {
                var toggleItem = choice as ToggleMenuItem;
                if (toggleItem != null)
                {
                    toggleItem.Down();
                    Menu.StartSound(Sfx.PISTOL);
                    return true;
                }

                var textToggleItem = choice as TextToggleMenuItem;
                if (textToggleItem != null)
                {
                    textToggleItem.Down();
                    Menu.StartSound(Sfx.PISTOL);
                    return true;
                }

                var sliderItem = choice as SliderMenuItem;
                if (sliderItem != null)
                {
                    sliderItem.Down();
                    Menu.StartSound(Sfx.STNMOV);
                    return true;
                }

                var textSliderItem = choice as TextSliderMenuItem;
                if (textSliderItem != null)
                {
                    textSliderItem.Down();
                    Menu.StartSound(Sfx.STNMOV);
                    return true;
                }

                var choiceItem = choice as ChoiceMenuItem;
                if (choiceItem != null)
                {
                    choiceItem.Down();
                    Menu.StartSound(Sfx.PISTOL);
                }

                return true;
            }

            if (e.Key == DoomKey.Right)
            {
                var toggleItem = choice as ToggleMenuItem;
                if (toggleItem != null)
                {
                    toggleItem.Up();
                    Menu.StartSound(Sfx.PISTOL);
                    return true;
                }

                var textToggleItem = choice as TextToggleMenuItem;
                if (textToggleItem != null)
                {
                    textToggleItem.Up();
                    Menu.StartSound(Sfx.PISTOL);
                    return true;
                }

                var sliderItem = choice as SliderMenuItem;
                if (sliderItem != null)
                {
                    sliderItem.Up();
                    Menu.StartSound(Sfx.STNMOV);
                    return true;
                }

                var textSliderItem = choice as TextSliderMenuItem;
                if (textSliderItem != null)
                {
                    textSliderItem.Up();
                    Menu.StartSound(Sfx.STNMOV);
                    return true;
                }

                var choiceItem = choice as ChoiceMenuItem;
                if (choiceItem != null)
                {
                    choiceItem.Up();
                    Menu.StartSound(Sfx.PISTOL);
                }

                return true;
            }

            if (e.Key == DoomKey.Enter)
            {
                var toggleItem = choice as ToggleMenuItem;
                if (toggleItem != null)
                {
                    toggleItem.Up();
                    Menu.StartSound(Sfx.PISTOL);
                    return true;
                }

                var textToggleItem = choice as TextToggleMenuItem;
                if (textToggleItem != null)
                {
                    textToggleItem.Up();
                    Menu.StartSound(Sfx.PISTOL);
                    return true;
                }

                var simpleItem = choice as SimpleMenuItem;
                if (simpleItem != null)
                {
                    if (simpleItem.Selectable)
                    {
                        if (simpleItem.Action != null)
                        {
                            simpleItem.Action();
                        }

                        if (simpleItem.Next != null)
                        {
                            Menu.SetCurrent(simpleItem.Next);
                        }
                        else
                        {
                            Menu.Close();
                        }
                    }
                    Menu.StartSound(Sfx.PISTOL);
                    return true;
                }

                if (choice.Next != null)
                {
                    Menu.SetCurrent(choice.Next);
                    Menu.StartSound(Sfx.PISTOL);
                }
                return true;
            }

            if (e.Key == DoomKey.Escape)
            {
                if (Parent != null)
                {
                    Menu.SetCurrent(Parent);
                    Menu.StartSound(Sfx.PISTOL);
                }
                else
                {
                    Menu.Close();
                    Menu.StartSound(Sfx.SWTCHX);
                }
            }

            return true;
        }

        public IReadOnlyList<string> Name => name;
        public IReadOnlyList<int> TitleX => titleX;
        public IReadOnlyList<int> TitleY => titleY;
        public IReadOnlyList<MenuItem> Items => items;
        public MenuItem Choice => choice;
        
        public MenuDef Parent
        {
            get => parent;
            set => parent = value;
        }
    }
}
