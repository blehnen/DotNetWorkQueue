// ---------------------------------------------------------------------
// Copyright (c) 2014 John Atten
// http://www.codeproject.com/Articles/816301/Csharp-Building-a-Useful-Extensible-NET-Console-Ap
// ---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleShared
{
    public static class ConsoleExecute
    {
        private static ConsoleMacro _consoleMacro;

        public static void CancelMacroCapture()
        {
            _consoleMacro = null;
        }
        public static void StartMacroCapture()
        {
            _consoleMacro = new ConsoleMacro();
        }

        public static ConsoleExecuteResult SaveMacro(string name)
        {
            if (_consoleMacro == null) return new ConsoleExecuteResult("Nothing to save");
            //remove the last command, since that was the 'save' command
            _consoleMacro.RemoveLast();
            var result = _consoleMacro.Save(name);
            _consoleMacro = null;
            return result;
        }

        public static IEnumerable<ConsoleExecuteResult> RunMacro(string name)
        {
            _consoleMacro = new ConsoleMacro();
            _consoleMacro.Load(name);
            _consoleMacro.Running = true;
            if (_consoleMacro.Count == 0)
            {
                yield return new ConsoleExecuteResult("Macro not found or it contains no commands", new ConsoleExecuteAction(ConsoleExecuteActions.None, null));
            }
            else
            {
                foreach (var command in _consoleMacro.Values.Where(command => !string.IsNullOrWhiteSpace(command)))
                {
                    yield return
                        new ConsoleExecuteResult("command",
                            new ConsoleExecuteAction(ConsoleExecuteActions.RunCommand, command));
                }
            }
            _consoleMacro = null;
        }

        public static bool IsAsync(ConsoleCommand command,
            CommandList commands)
        {
            // Validate the command name:
            var found = false;
            Dictionary<string, ConsoleExecutingMethod> methodDictionary = null;
            foreach (var key in commands.Keys.Where(key => key.Namespace == command.LibraryClassName))
            {
                found = true;
                methodDictionary = commands[key];
                break;
            }
            if (!found)
            {
                return false;

            }
            if (!methodDictionary.ContainsKey(command.Name))
            {
                var newCommand = char.ToUpperInvariant(command.Name[0]) + command.Name.Substring(1);
                if (!methodDictionary.ContainsKey(newCommand))
                {
                    return false;
                }
                command.Name = char.ToUpperInvariant(command.Name[0]) + command.Name.Substring(1);
            }
            return methodDictionary[command.Name].Async;
        }
        public static async Task<ConsoleExecuteResult> ExecuteAsync(ConsoleCommand command,
            CommandList commands)
        {
            if (!ValidateCommand(command, commands, out var instance, out Dictionary<string, ConsoleExecutingMethod> methodDictionary))
            {
                return new ConsoleExecuteResult($"Unrecognized command \'{command.LibraryClassName}.{command.Name}\'. " +
                                       "Please type a valid command.");
            }
            if (BuildCommand(command, instance, methodDictionary,
                out object[] inputArgs,
                out Type typeInfo,
                out string errorMessage))
            {

                // This will throw if the number of arguments provided does not match the number 
                // required by the method signature, even if some are optional:
                int? id = -1;
                try
                {
                    //add the command to the current macro if capture is enabled
                    id = _consoleMacro?.Add(command.RawCommand);
                    var result = await ((Task<ConsoleExecuteResult>)typeInfo.InvokeMember(
                        command.Name,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod,
                        null, instance.Instance, inputArgs)).ConfigureAwait(false);
                    return result;
                }
                catch (TargetInvocationException ex)
                {
                    //remove bad commands from the macro
                    if (id.HasValue && id > -1)
                    {
                        _consoleMacro?.Remove(id.Value);
                    }
                    if (ex.InnerException != null)
                        throw ex.InnerException;
                    throw;
                }
            }
            return new ConsoleExecuteResult(errorMessage);
        }

        public static ConsoleExecuteResult Execute(ConsoleCommand command,
            CommandList commands)
        {
            if (!ValidateCommand(command, commands, out ConsoleExecutingAssembly instance, out Dictionary<string, ConsoleExecutingMethod> methodDictionary))
            {
                return new ConsoleExecuteResult($"Unrecognized command \'{command.LibraryClassName}.{command.Name}\'. " +
                                       "Please type a valid command.");
            }
            if (BuildCommand(command, instance, methodDictionary,
                out object[] inputArgs,
                out Type typeInfo,
                out string errorMessage))
            {

                // This will throw if the number of arguments provided does not match the number 
                // required by the method signature, even if some are optional:
                int? id = -1;
                try
                {
                    id = _consoleMacro?.Add(command.RawCommand);
                    var result = (ConsoleExecuteResult)typeInfo.InvokeMember(
                        command.Name,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod,
                        null, instance.Instance, inputArgs);
                    return result;
                }
                catch (TargetInvocationException ex)
                {
                    //remove bad commands from the macro
                    if (id.HasValue && id > -1)
                    {
                        _consoleMacro?.Remove(id.Value);
                    }
                    if (ex.InnerException != null)
                        throw ex.InnerException;
                    throw;
                }
            }
            return new ConsoleExecuteResult(errorMessage);
        }

        private static bool BuildCommand(ConsoleCommand command,
            ConsoleExecutingAssembly instance,
            IReadOnlyDictionary<string, ConsoleExecutingMethod> methodDictionary,
            out object[] inputArgs,
            out Type typeInfo,
            out string errorMessage)
        {
            inputArgs = null;
            typeInfo = null;
            errorMessage = null;

            // Make sure the correct number of required arguments are provided:
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            var methodParameterValueList = new List<object>();
            IEnumerable<ParameterInfo> paramInfoList = methodDictionary[command.Name].Parameters.ToList();

            // Validate proper # of required arguments provided. Some may be optional:
            var requiredParams = paramInfoList.Where(p => !p.IsOptional).ToList();
            var optionalParams = paramInfoList.Where(p => p.IsOptional).ToList();
            var requiredCount = requiredParams.Count;
            var optionalCount = optionalParams.Count;
            var providedCount = command.Arguments.Count();

            if (requiredCount > providedCount)
            {
                var message = new StringBuilder();
                message.AppendLine(
                    $"Missing required argument. {requiredCount} required, {optionalCount} optional, {providedCount} provided");

                if (requiredCount > 0)
                {
                    message.AppendLine("");
                    message.AppendLine("Required");
                    foreach (var required in requiredParams)
                    {
                        message.Append(required.Name);
                        message.Append(":");
                        message.AppendLine(required.ParameterType.ToString());
                    }
                }

                if (optionalParams.Count > 0)
                {
                    message.AppendLine("");
                    message.AppendLine("Optional");
                    foreach (var required in optionalParams)
                    {
                        message.Append(required.Name);
                        message.Append(":");
                        message.Append(required.ParameterType);
                        message.Append(":");
                        message.AppendLine(required.RawDefaultValue?.ToString() ?? "null");
                    }
                }

                errorMessage = message.ToString();
                return false;
            }

            // Make sure all arguments are coerced to the proper type, and that there is a 
            // value for every method parameter. The InvokeMember method fails if the number 
            // of arguments provided does not match the number of parameters in the 
            // method signature, even if some are optional:
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

            if (paramInfoList.Any())
            {
                // Populate the list with default values:
                methodParameterValueList.AddRange(paramInfoList.Select(param => param.DefaultValue));

                // Now walk through all the arguments passed from the console and assign 
                // accordingly. Any optional arguments not provided have already been set to 
                // the default specified by the method signature:
                for (var i = 0; i < command.Arguments.Count(); i++)
                {
                    var methodParam = paramInfoList.ElementAt(i);
                    var typeRequired = methodParam.ParameterType;
                    try
                    {
                        // Coming from the Console, all of our arguments are passed in as 
                        // strings. Coerce to the type to match the method parameter:
                        var value = ConsoleParseArgument.CoerceArgument(typeRequired, command.Arguments.ElementAt(i));
                        methodParameterValueList.RemoveAt(i);
                        methodParameterValueList.Insert(i, value);
                    }
                    // ReSharper disable once UncatchableException
                    catch (ArgumentException ex)
                    {
                        var argumentName = methodParam.Name;
                        var argumentTypeName = typeRequired.Name;
                        var message =
                            $"The value passed for argument '{argumentName}' cannot be parsed to type '{argumentTypeName}'";
                        throw new ArgumentException(message, ex);
                    }
                }
            }

            // Set up to invoke the method using reflection:
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // Need the full Namespace for this:
            var commandLibraryClass =
                instance.Instance.GetType();

            if (methodParameterValueList.Count > 0)
            {
                inputArgs = methodParameterValueList.ToArray();
            }
            typeInfo = commandLibraryClass;
            return true;
        }
        private static bool ValidateCommand(ConsoleCommand command,
            CommandList commands,
            out ConsoleExecutingAssembly instance,
            out Dictionary<string, ConsoleExecutingMethod> methodDictionary)
        {
            instance = null;
            methodDictionary = null;
            // Validate the command name:
            var found = false;
            foreach (var key in commands.Keys.Where(key => key.Namespace == command.LibraryClassName))
            {
                found = true;
                instance = key;
                methodDictionary = commands[key];
                break;
            }
            if (!found)
            {
                return false;

            }
            if (!methodDictionary.ContainsKey(command.Name))
            {
                var newCommand = char.ToUpperInvariant(command.Name[0]) + command.Name.Substring(1);
                if (!methodDictionary.ContainsKey(newCommand))
                {
                    return false;
                }
                command.Name = char.ToUpperInvariant(command.Name[0]) + command.Name.Substring(1);
            }
            return true;
        }
    }
}
