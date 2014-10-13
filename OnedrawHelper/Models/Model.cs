using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Threading;
using System.Threading.Tasks;

using Livet;
using CoreTweet;
using Newtonsoft.Json;
using OnedrawHelper.Data;

namespace OnedrawHelper.Models
{
    public class Model : NotificationObject
    {
        /*
         * NotificationObjectはプロパティ変更通知の仕組みを実装したオブジェクトです。
         */
        private readonly string ThemesPath = "themes.json";

        private Tokens Token { get; set; }
        public ObservableSynchronizedCollection<ThemeModel> Themes { get; set; }
        private DispatcherTimer Timer { get; set; }

        private Model()
        {
            Themes = new ObservableSynchronizedCollection<ThemeModel>();
            Timer = new DispatcherTimer() { Interval = TimeSpan.FromHours(2) };
        }

        ~Model()
        {
            //File.WriteAllText(ThemesPath, JsonConvert.SerializeObject(Themes.Select(p => p.Source)));
            Timer.Stop();
        }

        public void Initialize()
        {
            if (File.Exists(ThemesPath))
            {
                var sources = JsonConvert.DeserializeObject<IEnumerable<Theme>>(File.ReadAllText(ThemesPath))
                    .Select(p => new ThemeModel(p));
                foreach (var item in sources)
                    Themes.Add(item);
            }

            if (Token != null) UpdateThemes();
            Timer.Tick += (sender, e) => UpdateThemes();
            Timer.Start();
        }

        #region Singleton
        static Model _instance;
        public static Model Instance
        {
            get
            {
                if (_instance == null) _instance = new Model();
                return _instance;
            }
        }
        #endregion


        private void UpdateThemes()
        {
            Task.Run(() =>
            {
                foreach (var t in Themes)
                    t.UpdateNextChallenge(Token);
            });
        }

    }
}
