using System;

namespace FmShell.KeyHandler
{
    internal sealed class TabKeyHandler : IKeyHandler
    {
        public ConsoleKey HandledKey => ConsoleKey.Tab;

        public bool HandleKey(ConsoleKeyInfo keyInfo, Shell shell)
        {
            // TODO: Autocomplete behavior
            return false;
        }
    }
}
