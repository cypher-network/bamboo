// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

namespace Cli.UI
{
    public class UserInterfaceChoice
    {
        public UserInterfaceChoice(string text)
        {
            Text = text;
        }

        public string Text { get; }

        public override int GetHashCode()
        {
            return Text.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as UserInterfaceChoice);
        }

        public bool Equals(UserInterfaceChoice otherChoice)
        {
            return Text == otherChoice.Text;
        }
    }

    public class UserInterfaceSection
    {
        public UserInterfaceSection(string title, string description, UserInterfaceChoice[] choices)
        {
            Title = title;
            Description = description;
            Choices = choices;
        }

        public string Title { get; }
        public string Description { get; }
        public UserInterfaceChoice[] Choices { get; }
    }
}