namespace Clinic.Domain.Enums;

public static class AppointmentStatusExtensions
{
    // التحقق مما إذا كان الموعد في حالة نهائية لا تقبل التعديل
    public static bool IsFinalState(this AppointmentStatus status)
    {
        return status is AppointmentStatus.Completed or AppointmentStatus.Cancelled;
    }

    // التحقق من صحة الانتقال من الحالة الحالية إلى الحالة الجديدة
    public static bool CanTransitionTo(this AppointmentStatus currentStatus, AppointmentStatus newStatus)
    {
        if (currentStatus == newStatus)
            return false;

        return currentStatus switch
        {
            AppointmentStatus.Confirmed => newStatus is AppointmentStatus.Completed or AppointmentStatus.Cancelled,
            AppointmentStatus.Completed => false, 
            AppointmentStatus.Cancelled => false, 
            _ => false
        };
    }
}