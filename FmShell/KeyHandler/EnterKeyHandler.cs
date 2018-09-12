using System;

namespace FmShell.KeyHandler
{
    internal sealed class EnterKeyHandler
    {
        public bool HandleKey(ConsoleKeyInfo keyInfo, Shell shell)
        {
            shell.CursorIndex = 0;
            Console.WriteLine();
            return true;
        }
    }
}
