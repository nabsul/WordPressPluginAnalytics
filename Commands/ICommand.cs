using System.Threading.Tasks;
using WordPressPluginAnalytics.Lib;

namespace WordPressPluginAnalytics.Commands
{
    public interface ICommand
    {
        Task RunAsync(Config config);
    }
}
