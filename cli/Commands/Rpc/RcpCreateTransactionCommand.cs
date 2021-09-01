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
using BAMWallet.Model;

namespace Cli.Commands.Rpc
{
    class RpcCreateTransactionCommand : RpcBaseCommand
    {
        private WalletTransaction _transaction;
        public RpcCreateTransactionCommand(ref WalletTransaction tx, IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent, Session session)
            : base(serviceProvider, ref cmdFinishedEvent, session)
        {
            _transaction = tx;
        }

        public override void Execute(Session activeSession = null)
        {
            try
            {
                //pseudo code
                if(_commandReceiver.IsTransactionAllowed(_session))
                {
                    Result = _commandReceiver.CreateTransaction(_session, ref _transaction);
                }
                else
                {
                    Result = new Tuple<object, string>(null, "Transaction not allowed because a previous Transaction is pending");
                }
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
