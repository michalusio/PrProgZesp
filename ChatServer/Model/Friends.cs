using System.ComponentModel.DataAnnotations;

namespace ChatServer.Model
{
    public partial class Friends
    {
        public int Friend1 { get; set; }
        public int Friend2 { get; set; }
        [Key]
        public int Id { get; set; }

        public virtual Users Friend1Navigation { get; set; }
        public virtual Users Friend2Navigation { get; set; }
    }
}
