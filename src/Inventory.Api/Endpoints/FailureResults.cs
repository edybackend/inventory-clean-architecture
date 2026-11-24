using Inventory.Application;

namespace Inventory.Api.Endpoints;

/// <summary>Maps a use-case <see cref="Failure"/> to an RFC 9457 problem response. The only place that knows HTTP status codes.</summary>
internal static class FailureResults
{
    public static IResult ToProblem(this Failure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        if (failure.Kind is FailureKind.Validation && failure.Fields is not null)
        {
            return TypedResults.ValidationProblem(failure.Fields, detail: failure.Message);
        }

        var (status, title) = failure.Kind switch
        {
            FailureKind.NotFound => (StatusCodes.Status404NotFound, "Not found"),
            FailureKind.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            FailureKind.BusinessRule => (StatusCodes.Status422UnprocessableEntity, "Business rule violated"),
            _ => (StatusCodes.Status400BadRequest, "Invalid request"),
        };

        return TypedResults.Problem(
            detail: failure.Message,
            statusCode: status,
            title: title,
            extensions: failure.Details?.ToDictionary(pair => pair.Key, pair => pair.Value));
    }
}
