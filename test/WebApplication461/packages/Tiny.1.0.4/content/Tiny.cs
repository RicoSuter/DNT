using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

public class Tiny
{
    public readonly dynamic Arguments;

    public Tiny(IEnumerable<string> arguments)
    {
        Arguments = new ExpandoObject();
        var argumentDictionary = Arguments as IDictionary<string, object>;
        FillArguments(arguments, argumentDictionary, ':');
        argumentDictionary.Add("contains", new Func<string, bool>((argument) => { return (Arguments as IDictionary<string, object>).ContainsKey(argument); }));
    }

    public Tiny(IEnumerable<string> arguments, char separator)
    {
        Arguments = new ExpandoObject();
        var argumentDictionary = Arguments as IDictionary<string, object>;
        FillArguments(arguments, argumentDictionary, separator);
        argumentDictionary.Add("contains",new Func<string, bool>((argument) => { return (Arguments as IDictionary<string, object>).ContainsKey(argument); }));
    }

    private static void FillArguments(IEnumerable<string> arguments, IDictionary<string, object> argumentDictionary, char separator)
    {
        var undefinedCounter = 1;
        foreach (var argument in arguments)
        {
            string name, value;
            if (argument.Contains(separator.ToString(CultureInfo.InvariantCulture)))
            {
                var firstInstancePosition = argument.IndexOf(separator, 0);
                name = argument.Substring(0, firstInstancePosition);
                value = argument.Substring(firstInstancePosition + 1);
            }
            else
            {
                name = "UndefinedArgument" + undefinedCounter++;
                value = argument;
            }
            argumentDictionary.Add(name, value);
        }
        argumentDictionary.Add("count", arguments.Count());
    }
}
