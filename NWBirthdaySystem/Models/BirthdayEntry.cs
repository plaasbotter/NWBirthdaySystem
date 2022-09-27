using NWBirthdaySystem.Utils;

namespace NWBirthdaySystem.Models
{
    internal class BirthdayEntry
    {
        [PrimaryKey]
        public long Id { get; set; }
        public string Message { get; set; } = "";
        public long ChatId { get; set; }
    }
}
