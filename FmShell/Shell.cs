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
using FmShell.KeyHandler;

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
        public object ShellMethods { get; private set; }

        public string ConsoleTitle { get; private set; }
        public ConsoleColor? BackgroundColor { get; private set; }
        public ConsoleColor? ForegroundColor { get; private set; }

        public bool IsStarted { get; private set; }

        internal StringBuilder Characters { get; set; }
        internal short CursorIndex { get; set; }

        private IDictionary<LoggingRule, ICollection<Target>> RemovedTargets { get; set; }

        private IDictionary<ConsoleKey, Func<ConsoleKeyInfo, Shell, bool>> ConsoleKeyHandlers { get; set; }

        private volatile bool shouldRun;

        /// <summary>
        /// Constructs a new instance that delegates calls to the specified object and uses the provided string as
        /// an input prompt for the final user.
        /// </summary>
        /// <param name="shellMethods">The <see cref="object"/> to delegate invocations to.</param>
        /// <param name="shellPrompt">The <see cref="string"/> to prepend as a prompt for user input.</param>
        public Shell(object shellMethods, string shellPrompt = "#FmShell>", string consoleTitle = null, ConsoleColor? backgroundColor = null, ConsoleColor? foregroundColor = null)
        {
            ShellMethods = shellMethods;
            ShellPrompt = shellPrompt;
            ConsoleTitle = consoleTitle;
            BackgroundColor = backgroundColor;
            ForegroundColor = foregroundColor;
            Characters = new StringBuilder();
            RemovedTargets = new Dictionary<LoggingRule, ICollection<Target>>();
            // TODO: Reflection-based discovery?
            ConsoleKeyHandlers = new Dictionary<ConsoleKey, Func<ConsoleKeyInfo, Shell, bool>>()
            {
                { ConsoleKey.Backspace, new BackspaceKeyHandler().HandleKey },
                { ConsoleKey.Delete, new DeleteKeyHandler().HandleKey },
                { ConsoleKey.Tab, new TabKeyHandler().HandleKey },
                { ConsoleKey.DownArrow, new DownArrowKeyHandler().HandleKey },
                { ConsoleKey.UpArrow, new UpArrowKeyHandler().HandleKey },
                { ConsoleKey.RightArrow, new RightArrowKeyHandler().HandleKey },
                { ConsoleKey.LeftArrow, new LeftArrowKeyHandler().HandleKey },
                { ConsoleKey.Enter, new EnterKeyHandler().HandleKey },
            };
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
                object[] args;
                try
                {
                    (command, args) = ProcessCommand();
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
                InvokeCommand(objectType, command, args);
            }
        }

        private void InvokeCommand(Type objectType, string methodName, object[] args)
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
                    response = methodInfo.Invoke(ShellMethods, new object[] { new FmShellArguments(this, args) });
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

        private (string command, object[] args) ProcessCommand()
        {
            while (!ProcessKey())
            {
                ;
            }
            try
            {
                string line = Characters.ToString();
                return ParseCommandLine(line);
            }
            finally
            {
                Characters.Clear();
            }
        }

        private (string command, object[] args) ParseCommandLine(string line)
        {
            bool inQuotes = false;
            bool isEscaping = false;

            string[] tokens = line.Split(c => {
                if (c == '\\' && !isEscaping) { isEscaping = true; return false; }

                if (c == '\"' && !isEscaping)
                    inQuotes = !inQuotes;

                isEscaping = false;

                return !inQuotes && Char.IsWhiteSpace(c)/*c == ' '*/;
            })
            .Select(arg => arg.Trim().TrimMatchingQuotes('\"'))
            .Where(arg => !String.IsNullOrEmpty(arg))
            .ToArray();

            if (tokens.Length > 0)
            {
                return (tokens[0], tokens.Skip(1).ToArray());
            }
            else
            {
                return (String.Empty, Array.Empty<string>());
            }
        }

        private bool ProcessKey()
        {
            // TODO: Use task interrupts instead
            SpinWait.SpinUntil(() =>
                Console.KeyAvailable || !shouldRun
            );
            if (!shouldRun)
            {
                throw new OperationCanceledException();
            }
            var keyInfo = Console.ReadKey(true);
            return HandleKeyInfo(keyInfo);
        }

        private bool HandleKeyInfo(ConsoleKeyInfo keyInfo)
        {
            var keyCode = keyInfo.Key;
            ConsoleKeyHandlers.TryGetValue(keyCode, out var handlerFunc);
            if (handlerFunc != null)
            {
                return handlerFunc.Invoke(keyInfo, this);
            }
            else
            {
                return HandleCharacterInsertion(keyInfo);
            }
        }

        private bool HandleCharacterInsertion(ConsoleKeyInfo keyInfo)
        {
            char keyChar = keyInfo.KeyChar;
            if (keyChar == '\0')
            {
                return false;
            }
            if(keyChar == '\n')
            {
                return ConsoleKeyHandlers[ConsoleKey.Enter].Invoke(new ConsoleKeyInfo(keyChar, ConsoleKey.Enter, false, false, false), this);
            }
            Characters.Insert(CursorIndex, keyChar);
            CursorIndex += 1;
            Console.Write(keyChar);
            if (true) // TODO: Is the 'insert' key off?
            {
                this.RewriteLine();
            }
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