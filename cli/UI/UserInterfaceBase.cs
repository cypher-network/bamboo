// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

namespace Cli.UI
{
    public abstract class UserInterfaceBase : IUserInterface
    {
        public abstract UserInterfaceChoice Do(UserInterfaceSection section);
        public abstract bool Do<T>(IUserInterfaceInput<T> input, out T output);

        protected string _topic;
        public IUserInterface SetTopic(string topic)
        {
            _topic = topic;
            return this;
        }
    }
}