using PolicyPlatform.Domain.Common;
using Xunit;

namespace PolicyPlatform.Domain.Tests;

public class FieldValidationExceptionTests
{
    [Fact]
    public void Constructor_SetsFieldCodeAndMessage()
    {
        var ex = new FieldValidationException("policeReportNumber", "POLICE_REPORT_NUMBER_REQUIRED", "Required.");

        Assert.Equal("policeReportNumber", ex.Field);
        Assert.Equal("POLICE_REPORT_NUMBER_REQUIRED", ex.Code);
        Assert.Equal("Required.", ex.Message);
    }

    [Fact]
    public void IsDomainException()
    {
        var ex = new FieldValidationException("field", "CODE", "message");

        Assert.IsAssignableFrom<DomainException>(ex);
    }
}
