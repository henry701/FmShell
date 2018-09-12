using System;

namespace FmShell.KeyHandler
{
    internal sealed class LeftArrowKeyHandler : IKeyHandler
    {
        public ConsoleKey HandledKey => ConsoleKey.LeftArrow;

        public bool HandleKey(ConsoleKeyInfo keyInfo, Shell shell)
        {
            if (shell.CursorIndex <= 0)
            {
                return false;
            }
            ConsoleUtilities.BackCursor();
            shell.CursorIndex -= 1;
            return false;
        }
    }
}
