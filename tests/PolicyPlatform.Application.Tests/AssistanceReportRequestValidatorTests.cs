using PolicyPlatform.Application.Assistance;
using PolicyPlatform.Domain.Assistance;
using PolicyPlatform.Domain.Common;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class AssistanceReportRequestValidatorTests
{
    private static readonly DateTime Now = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Validate_UnknownIncidentType_ThrowsInvalidIncidentType()
    {
        var request = new CreateAssistanceReportRequest("NOT_A_TYPE", 52.2m, 21.0m, null, null);

        var ex = Assert.Throws<AssistanceDomainException>(() => AssistanceReportRequestValidator.Validate(request, Now));

        Assert.Equal(AssistanceErrorCodes.InvalidIncidentType, ex.Code);
    }

    [Fact]
    public void Validate_MissingGps_ThrowsGpsLocationRequired()
    {
        var request = new CreateAssistanceReportRequest("NO_FUEL", null, null, null, null);

        var ex = Assert.Throws<AssistanceDomainException>(() => AssistanceReportRequestValidator.Validate(request, Now));

        Assert.Equal(AssistanceErrorCodes.GpsLocationRequired, ex.Code);
    }

    [Fact]
    public void Validate_OutOfRangeGps_ThrowsInvalidCoordinates()
    {
        var request = new CreateAssistanceReportRequest("NO_FUEL", 200m, 21.0m, null, null);

        var ex = Assert.Throws<AssistanceDomainException>(() => AssistanceReportRequestValidator.Validate(request, Now));

        Assert.Equal(AssistanceErrorCodes.InvalidCoordinates, ex.Code);
    }

    [Fact]
    public void Validate_OccurredAtTooFarInFuture_ThrowsDomainException()
    {
        var occurredAt = Now.AddMinutes(6).ToString("O");
        var request = new CreateAssistanceReportRequest("FLAT_BATTERY", 52.2m, 21.0m, null, occurredAt);

        Assert.Throws<DomainException>(() => AssistanceReportRequestValidator.Validate(request, Now));
    }

    [Fact]
    public void Validate_ValidRequest_ReturnsParsedFields()
    {
        var request = new CreateAssistanceReportRequest("DISABLED_VEHICLE", 52.2297m, 21.0122m, 10m, null);

        var result = AssistanceReportRequestValidator.Validate(request, Now);

        Assert.Equal(IncidentType.DISABLED_VEHICLE, result.IncidentType);
        Assert.Equal(52.2297m, result.Gps.Latitude);
        Assert.Null(result.OccurredAt);
    }
}
