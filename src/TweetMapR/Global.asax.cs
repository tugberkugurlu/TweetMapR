using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Routing;
using TweetMapR.Hubs;
using TweetMapR.Infrastructure;

namespace TweetMapR {

    public class Global : System.Web.HttpApplication {

        public readonly static ConcurrentDictionary<string, object> State = new ConcurrentDictionary<string, object>();

        protected void Application_Start(object sender, EventArgs e) {

            RouteTable.Routes.MapHubs();
            Task.Factory.StartNew(() => FireUpTheWork());
        }

        private void FireUpTheWork() {

            //TODO: No exception handling so far. Handle it better.
            //TODO: Don't block as much as possible and don't use Result

            var globalStreamGroupName = "Global";

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            State["cts"] = cancellationTokenSource;

            var isCycleOn = true;

            using (TwitterConnector twitterConnector = new TwitterConnector()) { 

                //TODO: Response message might be other than 200
                //      if this is the case, the other parts of the code might not work
                //      as excpected. Handle this.
                var response = twitterConnector.GetLocationBasedConnection("-165.0,-75.0,165.0,75.0").Result;
                //var response = twitterConnector.GetSampleFirehoseConnection().Result;
                var contentResult = response.Content.ReadAsStreamAsync().Result;

                using (var streamReader = new StreamReader(contentResult, Encoding.UTF8)) {
                    
                    var clients = GlobalHost.ConnectionManager.GetHubContext<TwitterHub>().Clients;

                    var cts = (CancellationTokenSource)State["cts"];
                    while (!streamReader.EndOfStream && !cts.IsCancellationRequested) {
                        
                        var result = streamReader.ReadLine();
                        if (!string.IsNullOrEmpty(result)) {

                            //TODO: Handle the exceptions here.
                            var tweetJToken = JsonConvert.DeserializeObject<dynamic>(result);
                            var tweetObj = tweetJToken["text"];

                            if (tweetObj != null) {

                                var tweetText = tweetObj.ToString();
                                var userScreenName = tweetJToken["user"]["screen_name"].ToString();
                                var imageUrl = tweetJToken["user"]["profile_image_url_https"].ToString();

                                var tweet = new TweetMapR.Entities.Model.Tweet() { TweetText = tweetText, User = userScreenName, ImageUrl = imageUrl };

                                var coordinatesRoot = tweetJToken["coordinates"];

                                if (coordinatesRoot != null && !string.IsNullOrEmpty(coordinatesRoot.ToString())) {

                                    var coordinates = coordinatesRoot["coordinates"];
                                    tweet.Longitude = coordinates[0];
                                    tweet.Latitude = coordinates[1];
                                }

                                //broadcast tweet to global stream subscribers
                                clients.Group(globalStreamGroupName).broadcastTweet(tweet);
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