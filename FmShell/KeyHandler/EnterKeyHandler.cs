using System;

namespace FmShell.KeyHandler
{
    internal sealed class EnterKeyHandler : IKeyHandler
    {
        public ConsoleKey HandledKey => ConsoleKey.Enter;

        public bool HandleKey(ConsoleKeyInfo keyInfo, Shell shell)
        {
            shell.CursorIndex = 0;
            Console.WriteLine();
            return true;
        }
    }
}
