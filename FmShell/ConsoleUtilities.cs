using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FmShell
{
    internal static class ConsoleUtilities
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

        public static void RewriteLine(this Shell shell)
        {
            int origLeft = Console.CursorLeft;
            int origTop = Console.CursorTop;
            for (int i = shell.CursorIndex; i < shell.Characters.Length; i++)
            {
                ConsoleUtilities.AdvanceCursor();
                Console.Write('\b');
                Console.Write(shell.Characters.ToString(i, 1));
            }
            ConsoleUtilities.AdvanceCursor();
            Console.Write("\b \b");
            Console.SetCursorPosition(origLeft, origTop);
        }
    }
}
