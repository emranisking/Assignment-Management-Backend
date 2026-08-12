namespace AssignmentManagement.Domain.Enums;

public enum UserRole
{
    Admin = 1,
    Teacher = 2,
    Student = 3
}

public enum CourseApplicationStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public enum ClassStatus
{
    Open = 1,
    Closed = 2,
    Cancelled = 3
}

public enum AssignmentStatus
{
    Draft = 1,
    Published = 2,
    Closed = 3
}

public enum EnrollmentStatus
{
    Active = 1,
    Dropped = 2
}

public enum EnrollmentRequestStatus
{
    Pending = 1,
    Processing = 2,
    Approved = 3,
    Rejected = 4
}

public enum SubmissionStatus
{
    Submitted = 1,
    Graded = 2,
    ResubmissionRequested = 3,
    Resubmitted = 4
}

public enum ResubmissionRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}
