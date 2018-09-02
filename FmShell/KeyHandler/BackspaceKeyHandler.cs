using System;
using System.Collections.Generic;
using System.Text;

namespace FmShell.KeyHandler
{
    internal sealed class BackspaceKeyHandler
    {
        public bool HandleKey(ConsoleKeyInfo keyInfo, Shell shell)
        {
            if (shell.CursorIndex <= 0)
            {
                return false;
            }
            shell.Characters.Remove(--(shell.CursorIndex), 1);
            Console.Write("\b \b");
            shell.RewriteLine();
            return false;
        }
    }
}
