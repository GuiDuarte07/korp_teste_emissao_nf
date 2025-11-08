using Microsoft.AspNetCore.Mvc;

namespace Shared.Common
{
    public static class ResultExtensions
    {
        public static IActionResult ToActionResult<T>(this Result<T> result)
        {
            if (result.IsSuccess)
            {
                return new OkObjectResult(result.Data);
            }

            return MapErrorToActionResult(result.ErrorCode!.Value, result.ErrorMessage);
        }

        public static IActionResult ToActionResult(this Result result)
        {
            if (result.IsSuccess)
            {
                return new OkResult();
            }

            return MapErrorToActionResult(result.ErrorCode!.Value, result.ErrorMessage);
        }

        public static IActionResult ToCreatedResult<T>(this Result<T> result, string actionName, object routeValues)
        {
            if (result.IsSuccess)
            {
                return new CreatedAtActionResult(actionName, null, routeValues, result.Data);
            }

            return MapErrorToActionResult(result.ErrorCode!.Value, result.ErrorMessage);
        }

        private static IActionResult MapErrorToActionResult(ErrorCode errorCode, string errorMessage)
        {
            var errorResponse = new { ErrorCode = errorCode.ToString(), ErrorMessage = errorMessage };

            return errorCode switch
            {
                ErrorCode.VALIDATION_ERROR => new BadRequestObjectResult(errorResponse),
                ErrorCode.INVALID_REQUEST => new BadRequestObjectResult(errorResponse),
                ErrorCode.INSUFFICIENT_STOCK => new BadRequestObjectResult(errorResponse),
                ErrorCode.DUPLICATE_CODE => new BadRequestObjectResult(errorResponse),
                ErrorCode.NOT_FOUND => new NotFoundObjectResult(errorResponse),
                ErrorCode.PRODUCT_NOT_FOUND => new NotFoundObjectResult(errorResponse),
                ErrorCode.RESERVATION_NOT_FOUND => new NotFoundObjectResult(errorResponse),
                ErrorCode.CONFLICT => new ConflictObjectResult(errorResponse),
                ErrorCode.ALREADY_CONFIRMED => new ConflictObjectResult(errorResponse),
                ErrorCode.ALREADY_CANCELLED => new ConflictObjectResult(errorResponse),
                ErrorCode.HAS_RESERVATIONS => new ConflictObjectResult(errorResponse),
                _ => new ObjectResult(errorResponse) { StatusCode = 500 }
            };
        }
    }
}
