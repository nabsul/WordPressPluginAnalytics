using System.Threading.Tasks;
using WordPressOrgCrawler.Lib;

namespace WordPressOrgCrawler.Commands
{
    public interface ICommand
    {
        Task RunAsync(Config config);
    }
}
