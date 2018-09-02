using System;
using System.Collections.Generic;
using System.Text;

namespace FmShell.KeyHandler
{
    internal sealed class DeleteKeyHandler
    {
        public bool HandleKey(ConsoleKeyInfo keyInfo, Shell shell)
        {
            if (shell.CursorIndex >= shell.Characters.Length)
            {
                return false;
            }
            shell.Characters.Remove(shell.CursorIndex, 1);
            shell.RewriteLine();
            return false;
        }
    }
}
