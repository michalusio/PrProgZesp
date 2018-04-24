using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ChatServer.Model
{
    public class WebChat : DbContext
    {
        public virtual DbSet<ConversationParticipants> ConversationParticipants { get; set; }
        public virtual DbSet<Conversations> Conversations { get; set; }
        public virtual DbSet<Messages> Messages { get; set; }
        public virtual DbSet<Users> Users { get; set; }
        public virtual DbSet<Friends> Friends { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(@"Data Source=tcp:127.0.0.1,63588;Database=WebChat;User Id=sa; Password=sa123;MultipleActiveResultSets=true");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConversationParticipants>(entity =>
            {
                entity.HasKey(e => e.Id).ForSqlServerIsClustered();
                entity.ToTable("ConversationParticipants");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.SeenMessage).HasColumnName("SeenMessage");

                entity.Property(e => e.ConversationId).HasColumnName("ConversationID");

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.HasOne(d => d.Conversation)
                    .WithMany(p => p.ConversationParticipants)
                    .HasForeignKey(d => d.ConversationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConversationParticipants_Conversations");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.ConversationParticipants)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Conversat__UserI__3D5E1FD2");
            });

            modelBuilder.Entity<Conversations>(entity =>
            {
                entity.ToTable("Conversations");
                entity.HasKey(e => e.Id).ForSqlServerIsClustered();
                entity.Property(e => e.Id).HasColumnName("ID");
            });

            modelBuilder.Entity<Friends>(entity =>
            {
                entity.ToTable("Friends");
                entity.HasKey(e => e.Id).ForSqlServerIsClustered();
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.HasOne(d => d.Friend1Navigation)
                    .WithMany(p => p.FriendsFriend1Navigation)
                    .HasForeignKey(d => d.Friend1)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Friends__Friend1__48CFD27E");

                entity.HasOne(d => d.Friend2Navigation)
                    .WithMany(p => p.FriendsFriend2Navigation)
                    .HasForeignKey(d => d.Friend2)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Friends__Friend2__49C3F6B7");
            });

            modelBuilder.Entity<Messages>(entity =>
            {
                entity.ToTable("Messages");
                entity.HasKey(e => e.Id).ForSqlServerIsClustered();
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.ConversationId).HasColumnName("ConversationID");

                entity.Property(e => e.Text)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.Timestamp)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.HasOne(d => d.Conversation)
                    .WithMany(p => p.Messages)
                    .HasForeignKey(d => d.ConversationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Messages_Conversations");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Messages)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Messages__UserID__412EB0B6");
            });

            modelBuilder.Entity<Users>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id).ForSqlServerIsClustered();
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Avatar).HasColumnType("image");

                entity.Property(e => e.CreationDate).HasColumnType("date");

                entity.Property(e => e.Email).HasMaxLength(50);

                entity.Property(e => e.Login)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.Nickname)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.Password).IsRequired();

                entity.Property(e => e.Phone).HasMaxLength(30);
            });
        }

        public Conversations GetConversationBetween(IQueryable<Users> users)
        {
            var usersIds = users
                .Select(u => u.Id)
                .OrderByDescending(x => x);
            var conversationIds = Conversations
                .Select(c => new
                {
                    Id = c,
                    users = c.ConversationParticipants
                        .Select(cp => cp.UserId)
                        .OrderByDescending(x => x)
                });
            Conversations conversationId = conversationIds
                .Where(c => c.users.SequenceEqual(usersIds))
                .Select(c => c.Id)
                .SingleOrDefault();
            return conversationId;
        }

        public IEnumerable<Conversations> GetConversationsInOrder(Users user)
        {
            return Users
                .Include("ConversationParticipants")
                .Include("ConversationParticipants.Conversation")
                .Include("ConversationParticipants.Conversation.ConversationParticipants")
                .Include("ConversationParticipants.Conversation.ConversationParticipants.User")
                .Include("ConversationParticipants.Conversation.Messages")
                .Single(u=>u.Id==user.Id)
                .ConversationParticipants
                .Select(c => c.Conversation)
                .OrderByDescending(c => 
                    c.Messages.Count>0?
                        c.Messages.Max(m => m.Timestamp):
                        new DateTime(1900,1,1)
                );
        }
    }
}
