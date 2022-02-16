// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

namespace Cli.UI
{
    public interface IUserInterface
    {
        public UserInterfaceChoice Do(UserInterfaceSection section);
        public bool Do<T>(IUserInterfaceInput<T> input, out T output);
        public IUserInterface SetTopic(string topic);
    }
}