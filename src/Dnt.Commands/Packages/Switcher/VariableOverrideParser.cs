using System;
using System.Collections;
using System.Collections.Generic;

namespace Dnt.Commands.Packages.Switcher
{
    /// <summary>Parses switcher variable overrides from the command line and environment variables.</summary>
    public static class VariableOverrideParser
    {
        public const string EnvironmentVariablePrefix = "DNT_SWITCHER_VAR_";

        /// <summary>Parses a semicolon-delimited override spec such as <c>"A=../x;B=../y"</c>.
        /// Entries are split on the first '=' only (values may contain '='); empty entries are ignored;
        /// duplicate keys use the last value.</summary>
        public static Dictionary<string, string> Parse(string spec)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(spec))
                return result;

            foreach (var entry in spec.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var separatorIndex = entry.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                var name = entry.Substring(0, separatorIndex).Trim();
                var value = entry.Substring(separatorIndex + 1).Trim();
                if (name.Length == 0)
                    continue;

                result[name] = value;
            }

            return result;
        }

        /// <summary>Reads variable overrides from environment variables named <c>DNT_SWITCHER_VAR_&lt;NAME&gt;</c>.</summary>
        public static Dictionary<string, string> FromEnvironment(string prefix = EnvironmentVariablePrefix)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                var key = entry.Key as string;
                if (key == null || key.Length <= prefix.Length)
                    continue;

                if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var name = key.Substring(prefix.Length);
                if (name.Length == 0)
                    continue;

                result[name] = entry.Value as string ?? string.Empty;
            }

            return result;
        }
    }
}
