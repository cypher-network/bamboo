// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BAMWallet.HD;
using BAMWallet.Model;

namespace Cli.Commands.Rpc
{
    class RpcWalletReceiveCommand : RpcBaseCommand
    {
        private string _paymentId;
        public RpcWalletReceiveCommand(string paymentId, IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent, Session session)
            : base(serviceProvider, ref cmdFinishedEvent, session)
        {
            _paymentId = paymentId;
        }

        public override Task Execute(Session activeSession = null)
        {
            try
            {
                var receivePaymentResult = _commandReceiver.ReceivePayment(_session, _paymentId);
                if (receivePaymentResult.Item1 is null)
                {
                    Result = new Tuple<object, string>(null, receivePaymentResult.Item2);
                }
                else
                {
                    var balanceSheetResult = _commandReceiver.History(_session);
                    if (balanceSheetResult.Item1 is null)
                    {
                        Result = new Tuple<object, string>(null, balanceSheetResult.Item2);
                    }
                    else
                    {
                        var lastSheet = (balanceSheetResult.Item1 as List<BalanceSheet>).Last();
                        Result = new Tuple<object, string>(new
                        {
                            memo = lastSheet.Memo,
                            received = lastSheet.MoneyIn,
                            balance = $"{lastSheet.Balance}"
                        }, balanceSheetResult.Item2);
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
