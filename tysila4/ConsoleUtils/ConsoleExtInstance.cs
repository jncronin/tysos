﻿using System;
using ConsoleUtils.ConsoleActions;

namespace ConsoleUtils
{
    internal class ConsoleExtInstance : IConsole
    {
        public PreviousLineBuffer PreviousLineBuffer { get { return ConsoleExt.PreviousLineBuffer; } }
        public string CurrentLine { get { return ConsoleExt.CurrentLine; } set { ConsoleExt.CurrentLine = value; } }
        public int CursorPosition { get { return Console.CursorLeft; } set { Console.CursorLeft = value; } }
    }
}
