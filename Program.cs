using System;
using System.Linq;
using System.Threading.Tasks;
using WordPressPluginAnalytics.Commands;
using System.Reflection;
using WordPressPluginAnalytics.Lib;
using System.Text.RegularExpressions;

namespace WordPressPluginAnalytics
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var program = new Program();
                program.MainAsync(args[0]).Wait();
            }
            catch(Exception e)
            {
                Console.WriteLine($"Failure:\n{e.ToString()}");
            }
        }

        async Task MainAsync(string command)
        {
            var assembly = Assembly.GetEntryAssembly();
            var config = new Config();            
            var commandTypes = from type in assembly.GetTypes()
                               where type.Namespace == "WordPressPluginAnalytics.Commands"
                               where !type.GetTypeInfo().IsAbstract
                               where type.GetInterfaces().Contains(typeof(ICommand))
                               select type;

            var lookup = commandTypes.ToDictionary(GetCommandName, t => t);
            if(lookup.TryGetValue(command, out var commandType))
            {
                var commandObject = (ICommand)assembly.CreateInstance(commandType.FullName);
                await commandObject.RunAsync(config);
                return;
            }

            var commandList = string.Join(", ", lookup.Keys);
            Console.WriteLine($"Commands: {commandList}");
        }

        private static string GetCommandName(Type type)
        {
            var command = type.Name;
            command = command.Replace("Command", "");
            command = Regex.Replace(command, "([A-Z])", "-$1");
            command = command.Substring(1).ToLower();
            command = command.Replace("word-press", "wordpress");
            return command;
        }
    }
}
