using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TweetMapR.Pusher;

[assembly: PreApplicationStartMethod(typeof(Startup), "Initialize")]
namespace TweetMapR.Pusher {

    public static class Startup {

        public static void Initialize() { 
        }
    }
}