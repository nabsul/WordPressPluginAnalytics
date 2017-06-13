using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Devsense.PHP.Syntax;

namespace WordPressPluginAnalytics.Lib
{
    public class ZipExtractor : IDisposable
    {
        private Stream _input;
        private string _fileName;
        private HashSet<string> HookFunctions = new HashSet<string> { "do_action", "apply_filters", "add_action", "add_filter" };
        private string OutputLine(string plugin, string file, int position, string type, string value)
        {
            return $"{_fileName}\t{plugin}\t{file}\t{position}\t{type}\t{value}\n";
        }

        public ZipExtractor(string fileName, Stream input)
        {
            _input = input;
            _fileName = fileName.Split('/').Last();
        }

        public IEnumerable<string> Extract()
        {
            try
            {
                return ProcessZipFile(_input);
            }
            catch (Exception e)
            {
                return new string[] { OutputLine("error", "error", 0, "error", e.ToString()) };
            }
        }

        private IEnumerable<string> ProcessZipFile(Stream input)
        {
            using (var archive = new ZipArchive(input))
            {
                var phpFiles = archive.Entries.Where(e => e.FullName.EndsWith(".php"));
                foreach (var row in phpFiles.SelectMany((entry) => GetRows(entry)))
                {
                    yield return row;
                }
            }
        }

        private IEnumerable<string> GetRows(ZipArchiveEntry entry)
        {
            IEnumerable<string> tokens;
            try
            {
                tokens = ProcessZipFileEntry(entry);
            }
            catch (Exception e)
            {
                tokens = new string[] { OutputLine(entry.FullName.Split('/')[0], entry.FullName, 0, "error", e.ToString()) };
            }

            return tokens;
        }

        IEnumerable<string> ProcessZipFileEntry(ZipArchiveEntry entry)
        {
            string plugin = entry.FullName.Split('/')[0];
            string file = entry.FullName;
            using (var stream = new StreamReader(entry.Open()))
            {
                foreach (var line in GetTokens(plugin, file, stream))
                {
                    yield return line;
                }
            }
        }

        private IEnumerable<string> GetTokens(string plugin, string fileName, StreamReader file)
        {
            var lex = new Lexer(file, Encoding.UTF8, null, LanguageFeatures.Php71Set);
            var parser = new Parser();

            var it = GetTokens(lex).GetEnumerator();
            while (it.MoveNext())
            {
                var action = "function_call";

                if (it.Current == Tokens.T_FUNCTION)
                {
                    action = "function_def";
                    it.MoveNext();
                }

                if (it.Current != Tokens.T_STRING)
                    continue;

                string value = lex.TokenText;
                int position = lex.TokenPosition.Start;

                var nextToken = lex.GetNextToken();
                if (lex.TokenText != "(")
                    continue;

                // we now know this is a function call
                yield return OutputLine(plugin, fileName, position, action, value);

                // check if it's hook-related
                if (!HookFunctions.Contains(value))
                    continue;

                it.MoveNext();
                yield return OutputLine(plugin, fileName, position, value, GetHookValue(it.Current, lex.TokenText));
            }
        }

        private IEnumerable<Tokens> GetTokens(Lexer lex)
        {
            Tokens token;
            int errorCount = 0;
            int lastPosition = 0;
            while((token = lex.GetNextToken()) != Tokens.EOF && 5 > errorCount)
            {
                // TODO: This is a temporary work around. Remove it when https://github.com/DEVSENSE/Parsers/issues/6 is resolved
                errorCount = lastPosition == lex.TokenPosition.Start ? ( errorCount + 1 ) : 0;
                lastPosition = lex.TokenPosition.Start;

                if (token == Tokens.T_WHITESPACE)
                    continue;

                yield return token;
            }
        }

        private string GetHookValue(Tokens token, string text)
        {
            if (token != Tokens.T_CONSTANT_ENCAPSED_STRING)
                return $"unknown:{token.ToString()}:{text}";

            return text.Trim('\'', '"');
        }

        public void Dispose()
        {
            _input.Dispose();
        }
    }
}

