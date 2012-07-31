using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TweetMapR.Model {
    
    public class Tweet {

        public string User { get; set; }
        public string TweetText { get; set; }
        public string CreatedAt { get; set; }
        public string ImageUrl { get; set; }
        public string Location { get; set; }
        public string Longitude { get; set; }
        public string Latitude { get; set; }
    }
}