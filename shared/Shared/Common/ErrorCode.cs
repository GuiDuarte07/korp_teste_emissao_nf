namespace Shared.Common
{
    public enum ErrorCode
    {
        // 400 - Bad Request
        VALIDATION_ERROR,
        INVALID_REQUEST,
        INSUFFICIENT_STOCK,
        DUPLICATE_CODE,
        
        // 404 - Not Found
        NOT_FOUND,
        PRODUCT_NOT_FOUND,
        RESERVATION_NOT_FOUND,
        
        // 409 - Conflict
        CONFLICT,
        ALREADY_CONFIRMED,
        ALREADY_CANCELLED,
        HAS_RESERVATIONS,
        
        // 500 - Internal Server Error
        INTERNAL_ERROR
    }
}
