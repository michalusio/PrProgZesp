using System;
using System.ComponentModel.DataAnnotations;

namespace ChatServer.Model
{
    public partial class Messages
    {
        public string Text { get; set; }
        public int UserId { get; set; }
        public int ConversationId { get; set; }
        public DateTime Timestamp { get; set; }
        [Key]
        public int Id { get; set; }

        public virtual Conversations Conversation { get; set; }
        public virtual Users User { get; set; }
    }
}
