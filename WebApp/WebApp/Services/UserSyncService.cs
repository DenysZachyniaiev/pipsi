using Microsoft.AspNetCore.Identity;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services
{
    public class UserSyncService
    {
        private readonly AppDbContext context;

        public UserSyncService(AppDbContext context)
        {
            this.context = context;
        }

        public async Task SyncStudentDataAsync(AppUser user, IList<string> roles)
        {
            bool isStudent = roles.Contains("Student");
            var existingStudent = context.Students.FirstOrDefault(s => s.Id == user.Id);

            if (isStudent && existingStudent == null)
            {
                context.Students.Add(new Student
                {
                    Id = user.Id,
                    FirstName = user.FirstName ?? "",
                    LastName = user.LastName ?? ""
                });
            }
            else if (!isStudent && existingStudent != null)
            {
                var classLinks = context.ClassStudents.Where(cs => cs.StudentId == user.Id);
                var grades = context.Grades.Where(g => g.StudentId == user.Id);
                var assignmentLinks = context.AssignmentStudents.Where(x => x.StudentId == user.Id);

                context.ClassStudents.RemoveRange(classLinks);
                context.Grades.RemoveRange(grades);
                context.AssignmentStudents.RemoveRange(assignmentLinks);
                context.Students.Remove(existingStudent);
            }

            await context.SaveChangesAsync();
        }
    }
}