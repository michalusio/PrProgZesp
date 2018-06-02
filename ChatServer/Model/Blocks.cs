using System.ComponentModel.DataAnnotations;

namespace ChatServer.Model
{
    public class Blocks
    {
        public int Block1 { get; set; }
        public int Block2 { get; set; }
        [Key]
        public int Id { get; set; }

        public virtual Users Block1Navigation { get; set; }
        public virtual Users Block2Navigation { get; set; }
    }
}
