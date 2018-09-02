using System;
using System.Linq;
using System.Reflection;
using NLog;
using System.Threading.Tasks;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NLog.Config;

namespace FmShell
{
    /// <summary>
    /// Uses the console input and output as a command shell, for administrating the host application.
    /// </summary>
    /// <threadsafety static="true" instance="true"/>
    public sealed class Shell
    {
        private static readonly ILogger LOGGER = LogManager.GetCurrentClassLogger();

        public string ShellPrompt { get; private set; }
        public Object ShellMethods { get; private set; }

        public string ConsoleTitle { get; private set; }
        public ConsoleColor? BackgroundColor { get; private set; }
        public ConsoleColor? ForegroundColor { get; private set; }

        public bool IsStarted { get; private set; }

        private StringBuilder Characters { get; set; }
        private short CursorIndex { get; set; }
        private IDictionary<LoggingRule, ICollection<Target>> RemovedTargets { get; set; }

        private volatile bool shouldRun;

        /// <summary>
        /// Constructs a new instance that delegates calls to the specified object and uses the provided string as
        /// an input prompt for the final user.
        /// </summary>
        /// <param name="shellMethods">The <see cref="object"/> to delegate invocations to.</param>
        /// <param name="shellPrompt">The <see cref="string"/> to prepend as a prompt for user input.</param>
        public Shell(Object shellMethods, string shellPrompt = "#FmShell>", string consoleTitle = null, ConsoleColor? backgroundColor = null, ConsoleColor? foregroundColor = null)
        {
            ShellMethods = shellMethods;
            ShellPrompt = shellPrompt;
            ConsoleTitle = consoleTitle;
            BackgroundColor = backgroundColor;
            ForegroundColor = foregroundColor;
            Characters = new StringBuilder();
            RemovedTargets = new Dictionary<LoggingRule, ICollection<Target>>();
        }

        private static bool IsConsoleTarget(Target target) => target is ConsoleTarget || target is ColoredConsoleTarget;

        private static bool IsConsoleTargetMaybeWrapped(Target target)
        {
            return IsConsoleTarget(target) || (target is AsyncTargetWrapper && IsConsoleTarget(((AsyncTargetWrapper)target).WrappedTarget));
        }

        /// <summary>
        /// Stops execution of the shell.
        /// </summary>
        public void Stop()
        {
            if(!IsStarted)
            {
                return;
            }

            shouldRun = false;
            Console.Out.WriteLine("Stopping shell...");

            SpinWait.SpinUntil(() =>
                !IsStarted
            );
        }

        /// <summary>
        /// Starts executing the shell.
        /// </summary>
        public void Start()
        {
            if(IsStarted)
            {
                return;
            }

            RemoveConsoleLoggers();
            InitializeConsole();

            Type objectType = ShellMethods.GetType();
            IsStarted = true;
            ExecutionLoop(objectType);

            RestoreConsoleLoggers();
            Console.Out.WriteLine("Shell has been stopped. Console Loggers have been re-added.");
            IsStarted = false;
        }

        private void ExecutionLoop(Type objectType)
        {
            shouldRun = true;
            while (shouldRun)
            {
                Console.Out.Write(ShellPrompt);
                string command;
                try
                {
                    command = ProcessCommand();
                }
                catch(OperationCanceledException)
                {
                    continue;
                }
                Characters.Clear();
                if (String.IsNullOrWhiteSpace(command))
                {
                    continue;
                }
                InvokeCommand(objectType, command);
            }
        }

        private void InvokeCommand(Type objectType, String methodName)
        {
            try
            {
                MethodInfo methodInfo = objectType.GetMethod(methodName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(FmShellArguments) }, new ParameterModifier[] { });
                if (methodInfo == null)
                {
                    Console.Out.WriteLine("Unknown command '" + methodName + "'");
                    return;
                }
                object response;
                try
                {
                    response = methodInfo.Invoke(ShellMethods, new object[] { new FmShellArguments(this, new object[] { }) });
                    Console.Out.WriteLine(Convert.ToString(response));
                }
                catch (Exception e)
                {
                    LOGGER.Warn(e, "Exception while running command with name '{}'", methodName);
                    Console.Out.WriteLine(e);
                }
            }
            catch (Exception e)
            {
                LOGGER.Debug(e, "Logger exception while finding method with name '{}'", methodName);
                Console.Out.WriteLine(e);
            }
        }

        private void InitializeConsole()
        {
            Console.BackgroundColor = BackgroundColor ?? Console.BackgroundColor;
            Console.ForegroundColor = ForegroundColor ?? Console.ForegroundColor;
            Console.Title = ConsoleTitle ?? "FmShell";
            Console.Clear();
            CursorIndex = 0;
        }

        private string ProcessCommand()
        {
            while (!ProcessKey())
            {
                ;
            }

            try
            {
                return Characters.ToString();
            }
            finally
            {
                Characters.Clear();
            }
        }

        private bool ProcessKey()
        {
            SpinWait.SpinUntil(() => 
                Console.KeyAvailable || !shouldRun
            );
            if(!shouldRun)
            {
                throw new OperationCanceledException();
            }
            var keyInfo = Console.ReadKey(true);
            switch(keyInfo.Key)
            {
                case ConsoleKey.Backspace:
                    return HandleBackspaceKey();
                case ConsoleKey.Delete:
                    return HandleDeleteKey();
                case ConsoleKey.UpArrow:
                    return HandleUpArrowKey();
                case ConsoleKey.DownArrow:
                    return HandleDownArrowKey();
                case ConsoleKey.LeftArrow:
                    return HandleLeftArrowKey();
                case ConsoleKey.RightArrow:
                    return HandleRightArrowKey();
                case ConsoleKey.Enter:
                    return HandleEnterKey();
                case ConsoleKey.Tab:
                    return HandleTabKey();
                default:
                    return HandleCharacterInsertion(keyInfo.KeyChar);
            }            
        }

        private bool HandleUpArrowKey()
        {
            // TODO: Command history
            return false;
        }

        private bool HandleDownArrowKey()
        {
            // TODO: Command history
            return false;
        }

        private bool HandleCharacterInsertion(char keyChar)
        {
            if (keyChar == '\0')
            {
                return false;
            }
            if(keyChar == '\n')
            {
                return HandleEnterKey();
            }
            Characters.Insert(CursorIndex, keyChar);
            CursorIndex += 1;
            Console.Write(keyChar);
            return false;
        }

        private bool HandleEnterKey()
        {
            CursorIndex = 0;
            Console.WriteLine();
            return true;
        }

        private bool HandleRightArrowKey()
        {
            if (CursorIndex >= Characters.Length)
            {
                return false;
            }
            ConsoleUtilities.AdvanceCursor();
            CursorIndex += 1;
            return false;
        }

        private bool HandleLeftArrowKey()
        {
            if (CursorIndex <= 0)
            {
                return false;
            }
            ConsoleUtilities.BackCursor();
            CursorIndex -= 1;
            return false;
        }

        private bool HandleDeleteKey()
        {
            if (CursorIndex >= Characters.Length)
            {
                return false;
            }
            Characters.Remove(CursorIndex, 1);
            int origLeft = Console.CursorLeft;
            int origTop = Console.CursorTop;
            for (int i = CursorIndex; i < Characters.Length; i++)
            {
                ConsoleUtilities.AdvanceCursor();
                Console.Write('\b');
                Console.Write(Characters.ToString(i, 1));
            }
            ConsoleUtilities.AdvanceCursor();
            Console.Write("\b \b");
            Console.SetCursorPosition(origLeft, origTop);
            return false;
        }

        private bool HandleBackspaceKey()
        {
            if (CursorIndex <= 0)
            {
                return false;
            }
            Characters.Remove(--CursorIndex, 1);
            Console.Write("\b \b");
            return false;
        }

        private bool HandleTabKey()
        {
            // TODO: Autocomplete
            return false;
        }

        private void RemoveConsoleLoggers()
        {
            LogManager.Flush();
            LogManager.Configuration?.LoggingRules?.ToList().ForEach(rule =>
            {
                var targetsToRemove = new List<Target>(rule.Targets.Where(IsConsoleTargetMaybeWrapped).ToList());
                foreach (var target in targetsToRemove)
                {
                    rule.Targets.Remove(target);
                }
                RemovedTargets[rule] = targetsToRemove;
            });
            LogManager.Flush();
            LogManager.ReconfigExistingLoggers();
            LogManager.Flush();
        }

        private void RestoreConsoleLoggers()
        {
            LogManager.Flush();
            foreach(KeyValuePair<LoggingRule, ICollection<Target>> keyPair in RemovedTargets)
            {
                foreach(var target in keyPair.Value)
                {
                    keyPair.Key.Targets.Add(target);
                }
            }
            LogManager.Flush();
            LogManager.ReconfigExistingLoggers();
            LogManager.Flush();
        }
    }
}