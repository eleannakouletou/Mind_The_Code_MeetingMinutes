using MeetingMinutes.Models;
using System.Linq;

namespace MeetingMinutes.Data
{
    public static class DbInitializer
    {
        // RiskLevel rows are referenced by a required foreign key from MeetingItem,
        // so without them no meeting item can ever be saved.
        public static void Initialize(ApplicationDbContext context)
        {
            if (context.RiskLevels.Any())
            {
                return;
            }

            context.RiskLevels.AddRange(
                new RiskLevel { Name = "Low" },
                new RiskLevel { Name = "Medium" },
                new RiskLevel { Name = "High" });
            context.SaveChanges();
        }
    }
}
