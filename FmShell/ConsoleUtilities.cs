using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FmShell
{
    internal class ConsoleUtilities
    {
        public static void AdvanceCursor()
        {
            if (Console.CursorLeft + 1 < Console.BufferWidth)
            {
                Console.CursorLeft += 1;
            }
            else
            {
                if (Console.CursorTop < Console.BufferHeight)
                {
                    Console.SetCursorPosition(1, Console.CursorTop + 1);
                }
            }
        }

        public static void BackCursor()
        {
            if (Console.CursorLeft > 1)
            {
                Console.CursorLeft -= 1;
            }
            else
            {
                if (Console.CursorTop > 1)
                {
                    Console.SetCursorPosition(Console.BufferWidth - 1, Console.CursorTop - 1);
                }
            }
        }
    }
}
