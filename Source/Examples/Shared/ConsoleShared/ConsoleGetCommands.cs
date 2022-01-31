// ---------------------------------------------------------------------
// Copyright (c) 2014 John Atten
// http://www.codeproject.com/Articles/816301/Csharp-Building-a-Useful-Extensible-NET-Console-Ap
// ---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
namespace ConsoleShared
{
    public static class ConsoleGetCommands
    {
        public static CommandList GetCommands(Assembly assembly)
        {
            // Any static classes containing commands for use from the 
            // console are located in the Commands namespace. Load 
            // references to each type in that namespace via reflection:
            var commands = new CommandList();

            // Use reflection to load all of the classes in the Commands namespace:
            var current = Assembly.GetExecutingAssembly();
            var q = from t in current.GetTypes()
                    where t.IsClass
                    && typeof(IConsoleCommand).IsAssignableFrom(t)
                    select t;
            var defaultCommands = GetCommands(q, current);
            foreach (var command in defaultCommands.Where(command => !command.Key.Namespace.Contains("get_Info")))
            {
                commands.Add(command.Key, command.Value);
            }

            var user = from t in assembly.GetTypes()
                       where t.IsClass
                       && typeof(IConsoleCommand).IsAssignableFrom(t)
                       select t;

            var userCommands = GetCommands(user, assembly);
            foreach (var command in userCommands)
            {
                commands.Add(command.Key, command.Value);
            }

            return commands;
        }

        private static CommandList GetCommands(IEnumerable<Type> commandClasses, Assembly assembly)
        {
            var commands = new CommandList();
            foreach (var commandClass in commandClasses)
            {
                var instance = (IConsoleCommand)Activator.CreateInstance(commandClass);
                // Load the method info from each class into a dictionary:
                var methods = commandClass.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                var methodDictionary = new Dictionary<string, ConsoleExecutingMethod>();
                foreach (var method in methods)
                {
                    var commandName = method.Name;
                    if (commandName.Contains("get_Info")) continue;
                    if (method.ReturnParameter != null && method.DeclaringType != null &&
                        method.ReturnParameter.ParameterType.FullName.Contains("ConsoleShared.ConsoleExecuteResult"))
                    {
                        methodDictionary.Add(commandName,
                            new ConsoleExecutingMethod(method.DeclaringType.Namespace, assembly,
                                method.GetParameters(), IsAsyncMethod(method)));
                    }
                }

                // Add the dictionary of methods for the current class into a dictionary of command classes:
                commands.Add(new ConsoleExecutingAssembly(commandClass.Name, instance), methodDictionary);
            }
            return commands;
        }
        private static bool IsAsyncMethod(MemberInfo method)
        {
            var attributeType = typeof(AsyncStateMachineAttribute);
            var attribute = (AsyncStateMachineAttribute)method.GetCustomAttribute(attributeType);
            return attribute != null;
        }
    }
}
