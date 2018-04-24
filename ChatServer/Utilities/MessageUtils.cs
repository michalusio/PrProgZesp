namespace ChatServer.Utilities
{
    public static class MessageUtils
    {
        public static string Reformat(string msg)
        {
            return msg.Replace("<","<&nbsp;").Replace("\n", "<br/>");
        }
    }
}