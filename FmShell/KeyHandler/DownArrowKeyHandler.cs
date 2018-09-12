using System;

namespace FmShell.KeyHandler
{
    internal sealed class DownArrowKeyHandler : IKeyHandler
    {
        public ConsoleKey HandledKey => ConsoleKey.DownArrow;

        public bool HandleKey(ConsoleKeyInfo keyInfo, Shell shell)
        {
            // TODO: Command History navigate
            return false;
        }
    }
}
