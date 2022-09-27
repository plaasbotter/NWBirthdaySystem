using NWBirthdaySystem.Models;

namespace NWBirthdaySystem.Library
{
    internal interface IDatabaseContext
    {
        void CheckAndCreateTables(string tableName, object tableObject, bool forceDrop);
        void CreateTableWithObject(string tableName, object input);
        bool TestConnection();


        List<Birthday> GetBirthdays();
        long GetLastMessage();
        void UpsertMessage(BirthdayEntry tempBirthdayEntry);
        void UpsertBirthday(Birthday tempBirthday);
    }
}