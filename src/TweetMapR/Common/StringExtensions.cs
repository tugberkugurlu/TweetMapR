using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace System {

    internal static class StringExtensions {

        internal static string Link(this string s, string url) {
            
            return string.Format("<a href=\"{0}\" target=\"_blank\">{1}</a>", url, s);
        }

        internal static string ParseURL(this string s) {

            return Regex.Replace(s, 
                @"(http(s)?://)?([\w-]+\.)+[\w-]+(/\S\w[\w- ;,./?%&=]\S*)?", new MatchEvaluator(StringExtensions.URL));
        }

        internal static string ParseUsername(this string s) {

            return Regex.Replace(s,
                "(@)((?:[A-Za-z0-9-_]*))", new MatchEvaluator(StringExtensions.Username));
        }

        public static string ParseHashtag(this string s) {

            return Regex.Replace(s,
                "(#)((?:[A-Za-z0-9-_]*))", new MatchEvaluator(StringExtensions.Hashtag));
        }

        private static string Hashtag(Match m) {
            string x = m.ToString();
            string tag = x.Replace("#", "%23");
            return x.Link("http://search.twitter.com/search?q=" + tag);
        }

        private static string Username(Match m) {
            string x = m.ToString();
            string username = x.Replace("@", "");
            return x.Link("http://twitter.com/" + username);
        }

        private static string URL(Match m) {

            string x = m.ToString();
            return x.Link(x);
        }
    }
}