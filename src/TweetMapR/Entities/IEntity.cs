using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TweetMapR.Entities {

    public interface IEntity {

        Guid Key { get; set; }
    }
}