using NWBirthdaySystem.Models;

namespace NWBirthdaySystem.Library
{
    internal interface IMessageContext
    {
        MessageBase ReadMessages();
        bool SendMessage(string text, string chatId);
    }
}