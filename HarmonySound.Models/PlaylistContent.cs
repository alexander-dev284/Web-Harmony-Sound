using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonySound.Models
{
    public class PlaylistContent
    {
        public int PlaylistId { get; set; }
        public int ContentId { get; set; }
        public Playlist Playlist { get; set; }
        public Content Content { get; set; }
    }
}
