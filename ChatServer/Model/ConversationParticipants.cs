using System.ComponentModel.DataAnnotations;

namespace ChatServer.Model
{
    public partial class ConversationParticipants
    {
        public int UserId { get; set; }
        public int ConversationId { get; set; }
        [Key]
        public int Id { get; set; }

        public int SeenMessage { get; set; }

        public virtual Conversations Conversation { get; set; }
        public virtual Users User { get; set; }
    }
}
