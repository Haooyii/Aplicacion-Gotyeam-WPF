using System.Collections.Generic;

namespace Gotyeam.MVVM.Model
{
    public class RawgGameResult
    {
        public List<RawgGame> results { get; set; }
    }

    public class RawgGame
    {
        public int id { get; set; }
        public string name { get; set; }
        public string background_image { get; set; }
        public string released { get; set; }
        public double rating { get; set; }
    }

    // Detail endpoint gives description and developers
    public class RawgGameDetail : RawgGame
    {
        public string description_raw { get; set; }
        public List<RawgDeveloper> developers { get; set; }
        public List<RawgGenre> genres { get; set; }
    }

    public class RawgDeveloper
    {
        public string name { get; set; }
    }

    public class RawgGenre
    {
        public string name { get; set; }
    }
}
