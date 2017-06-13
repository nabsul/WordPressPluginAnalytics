using Devsense.PHP.Syntax;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WordPressOrgCrawler.Lib;

namespace WordPressOrgCrawler.Commands
{
    public class ParseLocalCommand : ICommand
    {
        public Task RunAsync(Config config)
        {
            using (var data = File.OpenRead(config.Args[1]))
            using (var stream = new StreamReader(data))
            {
                var lex = new Lexer(stream, Encoding.Unicode);
                Tokens token;
                while ((token = lex.GetNextToken()) != Tokens.EOF)
                {
                    Console.WriteLine($"{token.ToString()}:{lex.TokenText}:{token.GetTokenCategory(lex.CurrentLexicalState).ToString()}");
                }
            }
            return Task.FromResult(true);
        }
    }
}
