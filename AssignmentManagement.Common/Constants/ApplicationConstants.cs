namespace AssignmentManagement.Common.Constants;

public static class ApplicationConstants
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Teacher = "Teacher";
        public const string Student = "Student";
    }

    public static class Files
    {
        public const long MaxSubmissionBytes = 15 * 1024 * 1024; // 15 MB
        public const string AllowedContentType = "application/pdf";
        public const string AllowedExtension = ".pdf";
    }

    public static class Cache
    {
        // Key prefixes used to build/invalidate cache entries per entity type.
        public const string CoursePrefix = "courses";
        public const string ClassPrefix = "classes";
        public const string AssignmentPrefix = "assignments";
        public const string UserPrefix = "users";
    }
}
