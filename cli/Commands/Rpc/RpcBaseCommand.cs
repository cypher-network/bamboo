// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading;
using BAMWallet.HD;
using Cli.Commands.Common;

namespace Cli.Commands.Rpc
{
    [CommandDescriptor("", "")]
    public abstract class RpcBaseCommand : Command
    {
        protected AutoResetEvent _cmdFinishedEvent;
        protected Session _session;
        public RpcBaseCommand(IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent, Session session)
            : base(typeof(RpcBaseCommand), serviceProvider)
        {
            _cmdFinishedEvent = cmdFinishedEvent;
            _session = session;
            Result = new Tuple<object, string>(null, "Command not executed.");
        }

        public Tuple<object, string> Result { get; protected set;}

        public void Wait()
        {
            _cmdFinishedEvent.WaitOne();
        }
    }
}
