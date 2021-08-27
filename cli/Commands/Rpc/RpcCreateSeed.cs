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
using NBitcoin;

namespace Cli.Commands.Rpc
{
    class RpcCreateSeedCommand : RpcBaseCommand
    {
        private WordCount _seedCount;
        private WordCount _passCount;
        public RpcCreateSeedCommand(WordCount seedCount, WordCount passCount, IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent)
            : base(serviceProvider, ref cmdFinishedEvent, null)
        {
            _seedCount = seedCount;
            _passCount = passCount;
        }

        public override void Execute(Session activeSession = null)
        {
            try
            {
                var seed = _walletService.CreateSeed(_seedCount);
                var passphrase = _walletService.CreateSeed(_passCount);

                Result = new Tuple<object, string>(new
                {
                    seed,
                    passphrase
                }, String.Empty);
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
