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
    public class ChallengeViewModel : ViewModel
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

        public Challenge Source { get; private set; }
        private ProgressState _progressStatus;
        public ProgressState ProgressStatus
        {
            get { return _progressStatus; }
            set
            {
                if (_progressStatus == value) return;
                _progressStatus = value;
                RaisePropertyChanged();
                switch (ProgressStatus)
                {
                    case ProgressState.Waiting:
                        TimeFormat = TimeFormatEnum.StartTime;
                        break;
                    case ProgressState.Progressing:
                        TimeFormat = TimeFormatEnum.RemainingTime;
                        break;
                    case ProgressState.Ended:
                        TimeFormat = TimeFormatEnum.StartTime;
                        break;
                }
            }
        }
        private TimeFormatEnum _timeFormat;
        public TimeFormatEnum TimeFormat
        {
            get { return _timeFormat; }
            set
            {
                if (_timeFormat == value) return;
                _timeFormat = value;
                RaisePropertyChanged();
            }
        }
        public TimeSpan RemainingTime { get { return Source.StartTime.AddHours(1) - DateTime.Now; } }

        public ChallengeViewModel(Challenge source)
        {
            this.Source = source;
        }

        public void Initialize()
        {
        }

        public void Refresh()
        {
            var t = RemainingTime;
            if (t.TotalSeconds < 0)
                ProgressStatus = ProgressState.Ended;
            else if (t < TimeSpan.FromHours(1))
            {
                ProgressStatus = ProgressState.Progressing;
                RaisePropertyChanged("RemainingTime");
            }
            else
                ProgressStatus = ProgressState.Waiting;

        }
    }

    public enum TimeFormatEnum
    {
        StartTime = 0,
        RemainingTime = 1
    }

    public enum ProgressState
    {
        Waiting,
        Progressing,
        Ended
    }
}
