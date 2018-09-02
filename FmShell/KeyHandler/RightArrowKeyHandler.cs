using System;
using System.Collections.Generic;
using System.Text;

namespace FmShell.KeyHandler
{
    internal sealed class RightArrowKeyHandler
    {
        public bool HandleKey(ConsoleKeyInfo keyInfo, Shell shell)
        {
            if (shell.CursorIndex >= shell.Characters.Length)
            {
                return false;
            }
            ConsoleUtilities.AdvanceCursor();
            shell.CursorIndex += 1;
            return false;
        }
    }
}
