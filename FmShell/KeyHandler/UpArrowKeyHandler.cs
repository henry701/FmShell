using System;

namespace FmShell.KeyHandler
{
    internal sealed class UpArrowKeyHandler : IKeyHandler
    {
        public ConsoleKey HandledKey => ConsoleKey.UpArrow;

        public bool HandleKey(ConsoleKeyInfo keyInfo, Shell shell)
        {
            // TODO: Command History navigate
            return false;
        }
    }
}
