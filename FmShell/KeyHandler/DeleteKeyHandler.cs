using System;

namespace FmShell.KeyHandler
{
    internal sealed class DeleteKeyHandler : IKeyHandler
    {
        public ConsoleKey HandledKey => ConsoleKey.Delete;

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
