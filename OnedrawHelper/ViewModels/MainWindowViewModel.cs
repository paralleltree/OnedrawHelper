using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using OnedrawHelper.Models;
using OnedrawHelper.Data;

namespace OnedrawHelper.ViewModels
{
    public class MainWindowViewModel : ViewModel
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
        public bool IsAuthorized
        {
            get
            {
                if (model == null) return false;
                return model.IsAuthorized;
            }
        }
        public ReadOnlyDispatcherCollection<ThemeViewModel> Themes { get; private set; }
        private IEnumerator<ThemeViewModel> ThemesEnumerator { get; set; }
        public ThemeViewModel CurrentTheme
        {
            get
            {
                if (Themes == null) return null;
                try
                {
                    return ThemesEnumerator.Current;
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }
        }
        public bool IsUpdatedAny { get { return CanMoveNextTheme() && Themes.Any(p => p.IsUpdated); } }

        public void Initialize()
        {
            model = Model.Instance;

            Themes = ViewModelHelper.CreateReadOnlyDispatcherCollection(
                model.Themes,
                p => new ThemeViewModel(p),
                DispatcherHelper.UIDispatcher);
            CompositeDisposable.Add(new CollectionChangedEventListener(Themes,
                (sender, e) =>
                {
                    ThemesEnumerator = Themes.GetEnumerator();
                    ThemesEnumerator.MoveNext();
                    RaisePropertyChanged("CurrentTheme");
                    MoveNextThemeCommand.RaiseCanExecuteChanged();

                    switch (e.Action)
                    {
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                            foreach (ThemeViewModel item in e.NewItems)
                                CompositeDisposable.Add(new PropertyChangedEventListener(item,
                                    (p, q) =>
                                    {
                                        if (q.PropertyName == "IsUpdated")
                                        {
                                            if (((ThemeViewModel)p).IsUpdated) Messenger.Raise(new InteractionMessage("FlashWindow"));
                                            CurrentTheme.IsUpdated = false;
                                            RaisePropertyChanged("IsUpdatedAny");
                                        }
                                    }));
                            break;
                    }
                }));
            RaisePropertyChanged("Themes");

            var listener = new PropertyChangedEventListener(model);
            listener.Add((sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case "IsAuthorized":
                        RaisePropertyChanged("IsAuthorized");
                        break;
                }
            });
            CompositeDisposable.Add(listener);

            model.Initialize();
        }


        #region MoveNextThemeCommand
        private ViewModelCommand _MoveNextThemeCommand;

        public ViewModelCommand MoveNextThemeCommand
        {
            get
            {
                if (_MoveNextThemeCommand == null)
                {
                    _MoveNextThemeCommand = new ViewModelCommand(MoveNextTheme, CanMoveNextTheme);
                }
                return _MoveNextThemeCommand;
            }
        }

        public bool CanMoveNextTheme()
        {
            if (Themes == null) return false;
            return Themes.Count() >= 2;
        }

        public void MoveNextTheme()
        {
            if (!ThemesEnumerator.MoveNext())
            {
                ThemesEnumerator.Reset();
                ThemesEnumerator.MoveNext();
            }
            CurrentTheme.IsUpdated = false;
            RaisePropertyChanged("CurrentTheme");
        }
        #endregion

        #region AuthorizeTwitterCommand
        private ViewModelCommand _AuthorizeTwitterCommand;

        public ViewModelCommand AuthorizeTwitterCommand
        {
            get
            {
                if (_AuthorizeTwitterCommand == null)
                {
                    _AuthorizeTwitterCommand = new ViewModelCommand(AuthorizeTwitter);
                }
                return _AuthorizeTwitterCommand;
            }
        }

        public void AuthorizeTwitter()
        {
            Messenger.Raise(new TransitionMessage(new AuthorizeTwitterWindowViewModel(this.model), "AuthorizeTwitter"));
        }
        #endregion


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CompositeDisposable.Dispose();
        }
    }
}
