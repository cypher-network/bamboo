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

namespace Cli.Commands.Rpc
{
    class RpxWalletAddressCommand : RpcBaseCommand
    {
        public RpxWalletAddressCommand(IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent, Session session)
            : base(serviceProvider, ref cmdFinishedEvent, session)
        {
        }

        public override void Execute(Session activeSession = null)
        {
            try
            {
                Result = _walletService.Address(_session);
            }
            catch (Exception ex)
            {
                Result = new Tuple<object, string>(null, ex.Message);
            }
            finally
            {
                _cmdFinishedEvent.Set();
            }
        }
    }
}
