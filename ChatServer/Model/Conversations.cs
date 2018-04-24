using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChatServer.Model
{
    public partial class Conversations
    {
        public Conversations()
        {
            ConversationParticipants = new HashSet<ConversationParticipants>();
            Messages = new HashSet<Messages>();
        }
        [Key]
        public int Id { get; set; }

        public ICollection<ConversationParticipants> ConversationParticipants { get; set; }
        public ICollection<Messages> Messages { get; set; }
    }
}
