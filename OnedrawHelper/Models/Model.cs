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
        private readonly string SettingPath = "settings.json";
        public CoreTweet.Configurations TwitterConfigrations { get; private set; }

        private Tokens _token;
        private Tokens Token
        {
            get { return _token; }
            set
            {
                if (_token == value) return;
                _token = value;

                Task.Run(() =>
                {
                    if (Token != null)
                        TwitterConfigrations = Token.Help.Configuration();

                    try
                    {
                        AuthorizedUser = Token.Account.VerifyCredentials(include_entities => false, skip_status => true);
                    }
                    catch { AuthorizedUser = null; }
                    RaisePropertyChanged("IsAuthorized");
                    RaisePropertyChanged("AuthorizedUser");
                });
            }
        }
        public bool IsAuthorized { get { return Token != null; } }
        public CoreTweet.User AuthorizedUser { get; private set; }
        public ObservableSynchronizedCollection<ThemeModel> Themes { get; private set; }
        private DispatcherTimer UpdateTimer { get; set; }

        private Model()
        {
            Themes = new ObservableSynchronizedCollection<ThemeModel>();
            UpdateTimer = new DispatcherTimer() { Interval = TimeSpan.FromHours(2) };
        }

        ~Model()
        {
            UpdateTimer.Stop();
            //File.WriteAllText(ThemesPath, JsonConvert.SerializeObject(Themes.Select(p => p.Source)));
            if (Token == null) return;
            File.WriteAllText(SettingPath, JsonConvert.SerializeObject(new Dictionary<string, string>()
            {
                { "AccessToken", Token.AccessToken },
                { "AccessTokenSecret", Token.AccessTokenSecret }
            }));
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

            UpdateTimer.Tick += async (sender, e) => await UpdateThemes();

            Task.Run(() =>
            {
                if (File.Exists(SettingPath))
                {
                    var settings = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(SettingPath));
                    try
                    {
                        Token = Tokens.Create(Properties.Resources.ConsumerKey, Properties.Resources.ConsumerSecret, settings["AccessToken"], settings["AccessTokenSecret"]);
                    }
                    catch { }
                }
            })
            .ContinueWith(async t =>
            {
                await UpdateThemes();
                UpdateTimer.Start();
            });
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


        private async Task UpdateThemes()
        {
            try
            {
                await Token.Account.VerifyCredentialsAsync();
            }
            catch { Token = null; }
            if (Token == null) return;

            foreach (var t in Themes)
                await t.UpdateNextChallengeAsync(Token);
        }

        #region Twitter
        public async Task<OAuth.OAuthSession> CreateAuthorizeSession()
        {
            return await OAuth.AuthorizeAsync(Properties.Resources.ConsumerKey, Properties.Resources.ConsumerSecret);
        }

        public async Task<bool> Authorize(OAuth.OAuthSession session, string pin)
        {
            return await Task<bool>.Run(() =>
            {
                try
                {
                    var token = OAuth.GetTokens(session, pin);
                    this.Token = token;
                    UpdateThemes();
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<bool> UpdateStatus(string text, IEnumerable<string> paths)
        {
            if (Token == null) throw new InvalidOperationException("Twitterへの認証が行われていません。");
            var result = await Task.WhenAll(paths.Select(p => Token.Media.UploadAsync(media => new FileInfo(p))));

            if (result.Count() > 0)
            {
                try
                {
                    var s = await Token.Statuses.UpdateAsync(status => text, media_ids => result.Select(p => p.MediaId));
                }
                catch { return false; }
                return true;
            }
            else
            {
                try
                {
                    var s = await Token.Statuses.UpdateAsync(status => text);
                }
                catch { return false; }
                return true;
            }
        }
        #endregion

    }
}
