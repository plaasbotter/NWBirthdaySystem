using NWBirthdaySystem.Models;

namespace NWBirthdaySystem.Library
{
    internal interface IDatabaseContext
    {
        void CheckAndCreateTables(string tableName, object tableObject, bool forceDrop);
        void CreateTableWithObject(string tableName, object input);
        bool TestConnection();


        List<Birthday> GetBirthdays(DateTime date);
        long GetLastMessage();
        void UpsertMessage(BirthdayEntry tempBirthdayEntry);
        bool UpsertBirthday(Birthday tempBirthday);
    }
}