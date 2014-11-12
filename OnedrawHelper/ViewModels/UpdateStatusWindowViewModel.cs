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

using OnedrawHelper.Behaviors;
using OnedrawHelper.Models;
using OnedrawHelper.Data;
using CoreTweet;

namespace OnedrawHelper.ViewModels
{
    public class UpdateStatusWindowViewModel : ViewModel
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
        private Theme Theme { get; set; }
        private string _text;
        public string Text
        {
            get { return _text; }
            set
            {
                if (_text == value) return;
                _text = value;
                RaisePropertyChanged("RemainingLength");
                UpdateStatusCommand.RaiseCanExecuteChanged();
            }
        }
        public ObservableSynchronizedCollection<string> Paths { get; private set; }
        public int RemainingLength
        {
            get { return 140 - Text.Length - (Paths.Count() > 0 ? TwitterConfigrations.CharactersReservedPerMedia : 0); }
        }
        private CoreTweet.User _authorizedUser;
        public CoreTweet.User AuthorizedUser
        {
            get { return _authorizedUser; }
            private set
            {
                if (_authorizedUser == value) return;
                _authorizedUser = value;
                RaisePropertyChanged();
            }
        }
        private readonly CoreTweet.Configurations TwitterConfigrations;
        private bool _isSending;
        public bool IsSending
        {
            get { return _isSending; }
            private set
            {
                if (_isSending == value) return;
                _isSending = value;
                RaisePropertyChanged();
            }
        }
        public AcceptDropDescription DropDescription { get; private set; }

        public UpdateStatusWindowViewModel(Model model, Theme theme, CoreTweet.User user, CoreTweet.Configurations config)
            : this(model, theme, user, config, Enumerable.Empty<string>())
        {
        }

        public UpdateStatusWindowViewModel(Model model, Theme theme, CoreTweet.User user, CoreTweet.Configurations config, IEnumerable<string> paths)
        {
            this.model = model;
            this.Theme = theme;
            this.AuthorizedUser = user;
            this.TwitterConfigrations = config;
            Text = string.Format(" {0}{1}", theme.HashTag.Substring(0, 1) == "#" ? "" : "#", theme.HashTag);
            Paths = new ObservableSynchronizedCollection<string>(paths);

            CompositeDisposable.Add(new CollectionChangedEventListener(Paths,
                (sender, e) =>
                {
                    RaisePropertyChanged("RemainingLength");
                    UpdateStatusCommand.RaiseCanExecuteChanged();
                }));

            DropDescription = AcceptDropDescription.FileDropDescription((e) =>
            {
                var data = e.Data.GetData(System.Windows.DataFormats.FileDrop) as IEnumerable<string>;
                data = data.Where(p =>
                    System.Text.RegularExpressions.Regex.IsMatch(p,
                    @"\.(png|jpe?g|gif)\Z",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase));

                foreach (var item in data)
                    Paths.Add(item);
            });
            RaisePropertyChanged("DropDescription");
        }

        public void Initialize()
        {
        }


        #region RemovePathCommand
        private ListenerCommand<string> _RemovePathCommand;

        public ListenerCommand<string> RemovePathCommand
        {
            get
            {
                if (_RemovePathCommand == null)
                {
                    _RemovePathCommand = new ListenerCommand<string>(RemovePath, CanRemovePath);
                }
                return _RemovePathCommand;
            }
        }

        public bool CanRemovePath()
        {
            return Paths.Count() > 0;
        }

        public void RemovePath(string parameter)
        {
            Paths.Remove(parameter);
        }
        #endregion

        #region UpdateStatusCommand
        private ViewModelCommand _UpdateStatusCommand;

        public ViewModelCommand UpdateStatusCommand
        {
            get
            {
                if (_UpdateStatusCommand == null)
                {
                    _UpdateStatusCommand = new ViewModelCommand(UpdateStatus, CanUpdateStatus);
                }
                return _UpdateStatusCommand;
            }
        }

        public bool CanUpdateStatus()
        {
            return RemainingLength < 140 && RemainingLength >= 0;
        }

        public async void UpdateStatus()
        {
            var deleted = Paths.Where(p => !System.IO.File.Exists(p));
            if (deleted.Count() > 0)
            {
                Messenger.Raise(new InformationMessage("指定されたファイルの一部が見つかりませんでした。\n送信を中断します。", "ファイルが見つかりません", System.Windows.MessageBoxImage.Warning, "InformationMessage"));
                foreach (var item in deleted)
                    Paths.Remove(item);
                return;
            }

            IsSending = true;

            bool succeed = await model.UpdateStatus(Text, Paths);
            if (succeed)
                Messenger.Raise(new InteractionMessage("Close"));
            else
                Messenger.Raise(new InformationMessage("ツイートの送信に失敗しました。", "ツイート送信エラー", System.Windows.MessageBoxImage.Error, "InformationMessage"));

            IsSending = false;

        }
        #endregion
    }
}
