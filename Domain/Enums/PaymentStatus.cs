namespace Domain.Enums
{
    public enum PaymentStatus
    {
        Pending,
        Success,    //Paystack confirms via webhook
        Failed
    }
}
