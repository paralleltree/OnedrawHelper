using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using OnedrawHelper.Models;
using CoreTweet;

namespace OnedrawHelper.ViewModels
{
    public class AuthorizeTwitterWindowViewModel : ViewModel
    {
        /* コマンド、プロパティの定義にはそれぞれ 
         * 
         *  lvcom   : ViewModelCommand
         *  lvcomn  : ViewModelCommand(CanExecute無)
         *  llcom   : ListenerCommand(パラメータ有のコマンド)
         *  llcomn  : ListenerCommand(パラメータ有のコマンド・CanExecute無)
         *  lprop   : 変更通知プロパティ(.NET4.5ではlpropn)
         *  
         * を使用してください。
         * 
         * Modelが十分にリッチであるならコマンドにこだわる必要はありません。
         * View側のコードビハインドを使用しないMVVMパターンの実装を行う場合でも、ViewModelにメソッドを定義し、
         * LivetCallMethodActionなどから直接メソッドを呼び出してください。
         * 
         * ViewModelのコマンドを呼び出せるLivetのすべてのビヘイビア・トリガー・アクションは
         * 同様に直接ViewModelのメソッドを呼び出し可能です。
         */

        /* ViewModelからViewを操作したい場合は、View側のコードビハインド無で処理を行いたい場合は
         * Messengerプロパティからメッセージ(各種InteractionMessage)を発信する事を検討してください。
         */

        /* Modelからの変更通知などの各種イベントを受け取る場合は、PropertyChangedEventListenerや
         * CollectionChangedEventListenerを使うと便利です。各種ListenerはViewModelに定義されている
         * CompositeDisposableプロパティ(LivetCompositeDisposable型)に格納しておく事でイベント解放を容易に行えます。
         * 
         * ReactiveExtensionsなどを併用する場合は、ReactiveExtensionsのCompositeDisposableを
         * ViewModelのCompositeDisposableプロパティに格納しておくのを推奨します。
         * 
         * LivetのWindowテンプレートではViewのウィンドウが閉じる際にDataContextDisposeActionが動作するようになっており、
         * ViewModelのDisposeが呼ばれCompositeDisposableプロパティに格納されたすべてのIDisposable型のインスタンスが解放されます。
         * 
         * ViewModelを使いまわしたい時などは、ViewからDataContextDisposeActionを取り除くか、発動のタイミングをずらす事で対応可能です。
         */

        /* UIDispatcherを操作する場合は、DispatcherHelperのメソッドを操作してください。
         * UIDispatcher自体はApp.xaml.csでインスタンスを確保してあります。
         * 
         * LivetのViewModelではプロパティ変更通知(RaisePropertyChanged)やDispatcherCollectionを使ったコレクション変更通知は
         * 自動的にUIDispatcher上での通知に変換されます。変更通知に際してUIDispatcherを操作する必要はありません。
         */

        private Model model { get; set; }
        private OAuth.OAuthSession Session { get; set; }
        private string _pin;
        public string PinCode
        {
            get { return _pin; }
            set
            {
                if (_pin == value) return;
                _pin = value;
                RaisePropertyChanged();
                AuthorizeCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _isSessionCreating;
        public bool IsSessionCreating
        {
            get { return _isSessionCreating; }
            private set
            {
                if (_isSessionCreating == value) return;
                _isSessionCreating = value;
                RaisePropertyChanged();
            }
        }
        private bool _isSessionCreated;
        public bool IsSessionCreated
        {
            get { return _isSessionCreated; }
            private set
            {
                if (_isSessionCreated == value) return;
                _isSessionCreated = value;
                RaisePropertyChanged();
            }
        }
        private bool _isAuthorizing;
        public bool IsAuthorizing
        {
            get { return _isAuthorizing; }
            private set
            {
                if (_isAuthorizing == value) return;
                _isAuthorizing = value;
                RaisePropertyChanged();
            }
        }


        public AuthorizeTwitterWindowViewModel(Model model)
        {
            this.model = model;
        }

        public void Initialize()
        {
        }


        #region CreateSessionCommand
        private ViewModelCommand _CreateSessionCommand;

        public ViewModelCommand CreateSessionCommand
        {
            get
            {
                if (_CreateSessionCommand == null)
                {
                    _CreateSessionCommand = new ViewModelCommand(CreateSession);
                }
                return _CreateSessionCommand;
            }
        }

        public void CreateSession()
        {
            this.IsSessionCreating = true;
            Task.Run(async () =>
            {
                this.Session = await model.CreateAuthorizeSession();
                System.Diagnostics.Process.Start(Session.AuthorizeUri.AbsoluteUri);
                this.IsSessionCreated = true;
            });
        }
        #endregion

        #region AuthorizeCommand
        private ViewModelCommand _AuthorizeCommand;

        public ViewModelCommand AuthorizeCommand
        {
            get
            {
                if (_AuthorizeCommand == null)
                {
                    _AuthorizeCommand = new ViewModelCommand(Authorize, CanAuthorize);
                }
                return _AuthorizeCommand;
            }
        }

        public bool CanAuthorize()
        {
            return this.IsSessionCreated && this.PinCode.Length == 7;
        }

        public void Authorize()
        {
            this.IsAuthorizing = true;
            Task.Run(async () =>
            {
                bool result = await model.Authorize(this.Session, this.PinCode);
                this.IsAuthorizing = false;
                if (result)
                    this.Messenger.Raise(new InteractionMessage("Close"));
                else
                    this.Messenger.Raise(new InformationMessage("認証に失敗しました。\nPINコードを再度確認してください。", "認証エラー", System.Windows.MessageBoxImage.Error, "AuthorizeError"));
            });
        }
        #endregion

    }
}
