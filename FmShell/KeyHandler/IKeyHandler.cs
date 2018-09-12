using System;
using System.Collections.Generic;
using System.Text;

namespace FmShell.KeyHandler
{
    public interface IKeyHandler
    {
        bool HandleKey(ConsoleKeyInfo keyInfo, Shell shell);
        ConsoleKey HandledKey { get; }
    }
}
