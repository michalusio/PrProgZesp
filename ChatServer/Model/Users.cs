using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ChatServer.Model
{
    public partial class Users
    {
        public Users()
        {
            ConversationParticipants = new HashSet<ConversationParticipants>();
            FriendsFriend1Navigation = new HashSet<Friends>();
            FriendsFriend2Navigation = new HashSet<Friends>();
            BlocksBlock1Navigation = new HashSet<Blocks>();
            BlocksBlock2Navigation = new HashSet<Blocks>();
            Messages = new HashSet<Messages>();
        }
        [Key]
        public int Id { get; set; }
        public string Nickname { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public DateTime CreationDate { get; set; }
        public byte[] Avatar { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        public ICollection<ConversationParticipants> ConversationParticipants { get; set; }
        public ICollection<Friends> FriendsFriend1Navigation { get; set; }
        public ICollection<Friends> FriendsFriend2Navigation { get; set; }
        public ICollection<Blocks> BlocksBlock1Navigation { get; set; }
        public ICollection<Blocks> BlocksBlock2Navigation { get; set; }
        public ICollection<Messages> Messages { get; set; }

        public bool IsFriendWith(Users u)
        {
            return FriendsFriend1Navigation.Any(f => f.Friend1 == u.Id || f.Friend2 == u.Id) ||
                   FriendsFriend2Navigation.Any(f => f.Friend1 == u.Id || f.Friend2 == u.Id);
        }
        public bool IsBlockedWith(Users u)
        {
            return BlocksBlock1Navigation.Any(f => f.Block1 == u.Id || f.Block2 == u.Id) ||
                   BlocksBlock2Navigation.Any(f => f.Block1 == u.Id || f.Block2 == u.Id);
        }
    }
}
