﻿using NWBirthdaySystem.Utils;

namespace NWBirthdaySystem.Models
{
    internal class Birthday
    {
        [PrimaryKeyAutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; } = "";
        [PrimaryKey]
        public short BirthDate { get; set; }
        public string ChatId { get; set; } = "";
    }

    internal class BirthdayShort
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
    }
}
