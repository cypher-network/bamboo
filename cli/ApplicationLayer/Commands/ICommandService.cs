// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace CLi.ApplicationLayer.Commands
{
    public interface ICommandService : IHostedService
    {
        void RegisterCommand(ICommand command);
        void Execute(string arg);
        Task Exit();
    }
}
