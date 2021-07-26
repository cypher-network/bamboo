// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System.Threading.Tasks;
using BAMWallet.HD;
namespace CLi.ApplicationLayer.Commands
{
    public abstract class Command : ICommand
    {
        protected Command(string name, string description)
        {
            Name = name;
            Description = description;
            ActiveSession = null;
        }
        public static Session ActiveSession { get; protected set;}
        public string Name { get; set; }
        public string Description { get; set; }
        public abstract Task Execute();
    }
}
