// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading;
using System.Threading.Tasks;
using BAMWallet.Extensions;
using BAMWallet.HD;
using BAMWallet.Helper;
using NBitcoin;

namespace Cli.Commands.Rpc
{
    class RpcCreateWalletCommand : RpcBaseCommand
    {
        private readonly string _walletName;
        private readonly string _seed;
        private readonly string _pass;
        
        public RpcCreateWalletCommand(string walletName, string seed, string passphrase, IServiceProvider serviceProvider, ref AutoResetEvent cmdFinishedEvent)
            : base(serviceProvider, ref cmdFinishedEvent, null)
        {
            _walletName = walletName;
            _seed = seed;
            _pass = passphrase;
        }

        public override async Task Execute(Session activeSession = null)
        {
            try
            {
                string[] seedDefault = _commandReceiver.CreateSeed(WordCount.TwentyFour);
                string[] passPhraseDefault = _commandReceiver.CreateSeed(WordCount.Twelve);
                string joinMmnemonic = string.Join(" ", _seed ?? string.Join(' ', seedDefault));
                string joinPassphrase = string.Join(" ", _pass ?? string.Join(' ', passPhraseDefault));
                string id = await _commandReceiver.CreateWallet(joinMmnemonic.ToSecureString(), joinPassphrase.ToSecureString(), _walletName);
                var session = new Session(id.ToSecureString(), joinPassphrase.ToSecureString());

                Result = new Tuple<object, string>(new
                {
                    path = Util.WalletPath(id),
                    identifier = id,
                    seed = joinMmnemonic,
                    passphrase = joinPassphrase,
                    address = session.KeySet.StealthAddress
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
