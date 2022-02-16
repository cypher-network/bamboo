// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Model;


namespace Cli.Commands.Rpc
{
    class RpcSpendCommand : RpcBaseCommand
    {
        private WalletTransaction _transaction;
        public RpcSpendCommand(ref WalletTransaction tx, IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent, Session session)
            : base(serviceProvider, ref cmdFinishedEvent, session)
        {
            _transaction = tx;
        }

        public override Task Execute(Session activeSession = null)
        {
            try
            {
                var createPaymentResult = _commandReceiver.CreateTransaction(_session, ref _transaction);
                if (createPaymentResult.Item1 is null)
                {
                    Result = new Tuple<object, string>(null, createPaymentResult.Item2);
                }
                else
                {
                    var send = _commandReceiver.SendTransaction(_session, ref _transaction);
                    if (send.Item1 is null)
                    {
                        Result = new Tuple<object, string>(null, send.Item2);
                    }
                    else
                    {
                        var history = _commandReceiver.History(_session);
                        if (history.Item1 is null)
                        {
                            Result = new Tuple<object, string>(null, history.Item2);
                        }
                        else
                        {
                            Result = new Tuple<object, string>(new
                            {
                                balance = $"{(history.Item1 as IList<BalanceSheet>).Last().Balance}",
                                paymentId = _transaction.Transaction.TxnId.ByteToHex()
                            }, String.Empty);
                        }
                    }
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

            return Task.CompletedTask;
        }
    }
}
