using System;
using System.Collections.Generic;
using System.Text;

namespace FmShell.KeyHandler
{
    internal sealed class LeftArrowKeyHandler
    {
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
