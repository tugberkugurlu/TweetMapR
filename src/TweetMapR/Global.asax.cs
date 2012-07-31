using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SignalR;
using TweetMapR.Hubs;
using TweetMapR.Model;
using TwitterDoodle.Http;
using TwitterDoodle.OAuth;

namespace TweetMapR {

    public class Global : System.Web.HttpApplication {

        public readonly static ConcurrentDictionary<string, object> State = new ConcurrentDictionary<string, object>();

        protected void Application_Start(object sender, EventArgs e) {

            Task.Factory.StartNew(() => FireUpTheWork());
        }

        private void FireUpTheWork() {

            //TODO: No exception handling so far. Handle it better.
            //TODO: Don't block as much as possible and don't use Result

            var uri = "https://stream.twitter.com/1/statuses/sample.json";
            var consumerKey = ConfigurationManager.AppSettings["ConsumerKey"];
            var consumerSecret = ConfigurationManager.AppSettings["ConsumerSecret"];
            var token = ConfigurationManager.AppSettings["Token"];
            var tokenSecret = ConfigurationManager.AppSettings["TokenSecret"];
            var globalStreamGroupName = "Global";

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            State["cts"] = cancellationTokenSource;

            OAuthCredential creds = new OAuthCredential(consumerKey) { Token = token };
            OAuthSignatureEntity signatureEntity = new OAuthSignatureEntity(consumerSecret) { TokenSecret = tokenSecret };

            var isCycleOn = true;

            using (TwitterHttpClient client = new TwitterHttpClient(creds, signatureEntity)) {

                client.Timeout = TimeSpan.FromMilliseconds(-1);
                var response = client.GetStreamAsync(uri).Result;

                using (var streamReader = new StreamReader(response, Encoding.UTF8)) {

                    var clients = GlobalHost.ConnectionManager.GetHubContext<TwitterHub>().Clients;

                    var cts = (CancellationTokenSource)State["cts"];
                    while (!streamReader.EndOfStream && !cts.IsCancellationRequested) {

                        var result = streamReader.ReadLine();
                        if (!string.IsNullOrEmpty(result)) {

                            var tweetJToken = JsonConvert.DeserializeObject<dynamic>(result);
                            var tweetObj = tweetJToken["text"];

                            if (tweetObj != null) {

                                var tweetText = tweetObj.ToString();
                                var userScreenName = tweetJToken["user"]["screen_name"].ToString();
                                var imageUrl = tweetJToken["user"]["profile_image_url_https"].ToString();

                                var tweet = new Tweet() { TweetText = tweetText, User = userScreenName, ImageUrl = imageUrl };

                                var coordinatesRoot = tweetJToken["coordinates"];

                                if (coordinatesRoot != null && !string.IsNullOrEmpty(coordinatesRoot.ToString())) {

                                    var coordinates = coordinatesRoot["coordinates"];
                                    tweet.Longitude = coordinates[0];
                                    tweet.Latitude = coordinates[1];
                                }

                                //broadcast tweet to global stream subscribers
                                clients[globalStreamGroupName].broadcastTweet(tweet);
                            }
                        }
                    }
                    if (cts.IsCancellationRequested) {
                        isCycleOn = false;
                    }
                }
            }

            if (isCycleOn) {
                FireUpTheWork();
            }
        }
    }
}